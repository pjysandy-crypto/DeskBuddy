using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace DeskBuddy {

	public class ProjectItem {
		public string Id = Guid.NewGuid().ToString("N");
		public string Name = "";
		public string Description = "";
		public string ColorTag = "#548B70";
		public string Status = "진행 중";
		public string CreatedDate = "";
	}

	public class DiaryProjectEntry {
		public string Id = Guid.NewGuid().ToString("N");
		public string ProjectId = "";
		public string ProjectName = "";
		public string Content = "";
		public string Progress = "진행 중";
	}

	public class WorkDiaryItem {
		public string Id = Guid.NewGuid().ToString("N");
		public string Date = "";
		public string WorkMemo = "";
		public bool Rewarded = false;
		public List<DiaryProjectEntry> ProjectEntries = new List<DiaryProjectEntry>();
	}

	public static class DiaryCore {
		public static bool RewardDiary(WorkDiaryItem diary, Data d, PetForm pet, MainForm parent) {
			if (diary == null || diary.Rewarded) return false;
			diary.Rewarded = true;
			d.Coins += 25;
			d.InteractionExperience += 15;
			Store.Save();

			if (pet != null) {
				pet.Say("업무일지 작성 보상! +25 G, +15 EXP 획득!", false);
			}
			if (parent != null && !parent.IsDisposed) {
				parent.RefreshPage();
			}
			return true;
		}
		public static void Normalize(Data d) {
			if (d.Projects == null) d.Projects = new List<ProjectItem>();
			if (d.Diaries == null) d.Diaries = new List<WorkDiaryItem>();
			if (d.Projects.Count == 0) {
				d.Projects.Add(new ProjectItem {
					Name = "기본 프로젝트",
					Description = "일상 및 주요 업무",
					ColorTag = "#548B70",
					Status = "진행 중",
					CreatedDate = AppClock.Now.ToString("yyyy-MM-dd")
				});
			}
			foreach (var p in d.Projects) {
				if (String.IsNullOrEmpty(p.Id)) p.Id = Guid.NewGuid().ToString("N");
				if (String.IsNullOrEmpty(p.Status)) p.Status = "진행 중";
				if (String.IsNullOrEmpty(p.ColorTag)) p.ColorTag = "#548B70";
			}
			foreach (var diary in d.Diaries) {
				if (String.IsNullOrEmpty(diary.Id)) diary.Id = Guid.NewGuid().ToString("N");
				if (diary.ProjectEntries == null) diary.ProjectEntries = new List<DiaryProjectEntry>();
				foreach (var entry in diary.ProjectEntries) {
					if (String.IsNullOrEmpty(entry.Id)) entry.Id = Guid.NewGuid().ToString("N");
				}
			}
			MonthlyReports.Normalize(d);
		}

		public static WorkDiaryItem GetOrCreateDiary(Data d, string date) {
			Normalize(d);
			var item = d.Diaries.FirstOrDefault(x => x.Date == date);
			if (item == null) {
				item = new WorkDiaryItem { Date = date };
				d.Diaries.Add(item);
			}
			return item;
		}

		public static void GetTodoStats(Data d, DateTime date, out List<TaskItem> tasks, out int total, out int completed, out int percent) {
			tasks = d.Tasks.Where(t => Scheduler.Due(t).Date == date.Date).OrderBy(t => t.Done).ThenBy(t => Scheduler.Due(t)).ToList();
			total = tasks.Count;
			completed = tasks.Count(t => t.Done);
			percent = total == 0 ? 0 : (completed * 100) / total;
		}

		public static List<Tuple<string, DiaryProjectEntry>> GetEntriesByProject(Data d, string projectId, string projectName) {
			Normalize(d);
			var list = new List<Tuple<string, DiaryProjectEntry>>();
			foreach (var diary in d.Diaries.OrderByDescending(x => x.Date)) {
				if (diary.ProjectEntries == null) continue;
				foreach (var entry in diary.ProjectEntries) {
					if ((!String.IsNullOrEmpty(projectId) && entry.ProjectId == projectId) ||
							(!String.IsNullOrEmpty(projectName) && entry.ProjectName == projectName)) {
						list.Add(Tuple.Create(diary.Date, entry));
					}
				}
			}
			return list;
		}

		public static string EscapeCsv(string val) {
			if (val == null) return "\"\"";
			return "\"" + val.Replace("\"", "\"\"") + "\"";
		}

		public static string GenerateCsv(Data d, string targetProjectId) {
			Normalize(d);
			var sb = new StringBuilder();
			sb.AppendLine("프로젝트,작성일자,진행상태,Work Memo 내용");

			List<ProjectItem> targetProjects;
			if (String.IsNullOrEmpty(targetProjectId) || targetProjectId == "all") {
				targetProjects = d.Projects.ToList();
			} else {
				targetProjects = d.Projects.Where(p => p.Id == targetProjectId).ToList();
				if (targetProjects.Count == 0 && d.Projects.Count > 0) targetProjects = new List<ProjectItem> { d.Projects[0] };
			}

			foreach (var proj in targetProjects) {
				var entries = GetEntriesByProject(d, proj.Id, proj.Name);
				if (entries.Count == 0) {
					sb.AppendLine(String.Format("{0},{1},{2},{3}", EscapeCsv(proj.Name), EscapeCsv("-"), EscapeCsv(proj.Status), EscapeCsv("(작성된 메모 없음)")));
				} else {
					foreach (var item in entries) {
						sb.AppendLine(String.Format("{0},{1},{2},{3}",
							EscapeCsv(proj.Name),
							EscapeCsv(item.Item1),
							EscapeCsv(item.Item2.Progress ?? "진행 중"),
							EscapeCsv(item.Item2.Content ?? "")
						));
					}
				}
			}
			return sb.ToString();
		}

		public static string GenerateHtmlReport(Data d, string targetProjectId) {
			Normalize(d);
			List<ProjectItem> targetProjects;
			bool isAll = String.IsNullOrEmpty(targetProjectId) || targetProjectId == "all";
			if (isAll) {
				targetProjects = d.Projects.ToList();
			} else {
				targetProjects = d.Projects.Where(p => p.Id == targetProjectId).ToList();
				if (targetProjects.Count == 0 && d.Projects.Count > 0) targetProjects = new List<ProjectItem> { d.Projects[0] };
			}

			int totalMemos = 0;
			foreach (var p in targetProjects) {
				totalMemos += GetEntriesByProject(d, p.Id, p.Name).Count;
			}

			var sb = new StringBuilder();
			sb.AppendLine("<!DOCTYPE html>");
			sb.AppendLine("<html lang=\"ko\">");
			sb.AppendLine("<head>");
			sb.AppendLine("<meta charset=\"utf-8\">");
			sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
			sb.AppendLine("<title>DeskBuddy 업무일지 보고서</title>");
			sb.AppendLine("<style>");
			sb.AppendLine("* { box-sizing: border-box; margin: 0; padding: 0; }");
			sb.AppendLine("body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Malgun Gothic', sans-serif; background: #fdfbf7; color: #33261f; line-height: 1.5; padding: 24px; }");
			sb.AppendLine(".container { max-width: 900px; margin: 0 auto; background: #fff; border: 2px solid #50392f; border-radius: 8px; padding: 32px; box-shadow: 0 4px 16px rgba(0,0,0,0.06); }");
			sb.AppendLine(".no-print { display: flex; justify-content: space-between; align-items: center; background: #f8eed8; border: 1px solid #e0cfad; border-radius: 6px; padding: 12px 18px; margin-bottom: 24px; }");
			sb.AppendLine(".btn-print { background: #548b70; color: #fff; border: none; border-radius: 4px; padding: 8px 18px; font-size: 14px; font-weight: bold; cursor: pointer; transition: background 0.2s; }");
			sb.AppendLine(".btn-print:hover { background: #436e59; }");
			sb.AppendLine(".header { border-bottom: 3px double #50392f; padding-bottom: 16px; margin-bottom: 24px; }");
			sb.AppendLine(".header h1 { font-size: 24px; color: #46332e; margin-bottom: 6px; }");
			sb.AppendLine(".header .meta { font-size: 13px; color: #876e57; display: flex; gap: 16px; flex-wrap: wrap; }");
			sb.AppendLine(".summary-box { display: flex; gap: 12px; margin-bottom: 28px; }");
			sb.AppendLine(".summary-card { flex: 1; background: #faf6ee; border: 1px solid #ebdcc5; border-radius: 6px; padding: 12px 16px; }");
			sb.AppendLine(".summary-card .title { font-size: 12px; color: #876e57; }");
			sb.AppendLine(".summary-card .val { font-size: 20px; font-weight: bold; color: #548b70; margin-top: 4px; }");
			sb.AppendLine(".project-section { margin-bottom: 32px; page-break-inside: avoid; }");
			sb.AppendLine(".project-header { background: #eee2cc; border-left: 5px solid #548b70; padding: 10px 14px; display: flex; justify-content: space-between; align-items: center; border-radius: 0 4px 4px 0; margin-bottom: 12px; }");
			sb.AppendLine(".project-title { font-size: 16px; font-weight: bold; color: #46332e; }");
			sb.AppendLine(".project-desc { font-size: 12px; color: #6e5844; margin-top: 2px; }");
			sb.AppendLine(".badge { display: inline-block; padding: 3px 8px; border-radius: 3px; font-size: 12px; font-weight: bold; }");
			sb.AppendLine(".badge-progress { background: #faecd8; color: #b47800; border: 1px solid #e6c88e; }");
			sb.AppendLine(".badge-done { background: #e2f2e9; color: #2e7d4e; border: 1px solid #b8dfc9; }");
			sb.AppendLine("table { width: 100%; border-collapse: collapse; margin-bottom: 12px; font-size: 13px; }");
			sb.AppendLine("th { background: #f5ede0; color: #46332e; text-align: left; padding: 8px 10px; border-bottom: 2px solid #dfcfb8; font-weight: 600; }");
			sb.AppendLine("td { padding: 10px; border-bottom: 1px solid #f0e6d6; vertical-align: top; }");
			sb.AppendLine("tr:last-child td { border-bottom: none; }");
			sb.AppendLine(".memo-text { white-space: pre-wrap; word-break: break-all; color: #33261f; }");
			sb.AppendLine(".empty-memo { color: #9c8a77; font-style: italic; padding: 12px 10px; }");
			sb.AppendLine(".footer { margin-top: 40px; padding-top: 14px; border-top: 1px dashed #dfcfb8; text-align: center; font-size: 12px; color: #876e57; }");
			sb.AppendLine("@media print {");
			sb.AppendLine("  body { background: #fff; padding: 0; }");
			sb.AppendLine("  .container { max-width: 100%; border: none; box-shadow: none; padding: 0; }");
			sb.AppendLine("  .no-print { display: none !important; }");
			sb.AppendLine("  .project-section { page-break-inside: avoid; }");
			sb.AppendLine("}");
			sb.AppendLine("</style>");
			sb.AppendLine("</head>");
			sb.AppendLine("<body>");
			sb.AppendLine("<div class=\"container\">");
			sb.AppendLine("  <div class=\"no-print\">");
			sb.AppendLine("    <div><strong>PDF 저장 팁:</strong> 우측 [PDF로 저장 / 인쇄] 버튼을 누른 후 대상을 <strong>'PDF로 저장'</strong>으로 선택하시면 깔끔한 PDF 파일로 생성됩니다.</div>");
			sb.AppendLine("    <button class=\"btn-print\" onclick=\"window.print()\">PDF로 저장 / 인쇄</button>");
			sb.AppendLine("  </div>");
			sb.AppendLine("  <div class=\"header\">");
			sb.AppendLine("    <h1>DeskBuddy 업무일지 & Work Memo 보고서</h1>");
			sb.AppendLine("    <div class=\"meta\">");
			sb.AppendLine("      <span>출력 일시: " + AppClock.Now.ToString("yyyy-MM-dd HH:mm") + "</span>");
			sb.AppendLine("      <span>대상: " + (isAll ? "전체 프로젝트 모아보기 (" + targetProjects.Count + "개)" : targetProjects[0].Name) + "</span>");
			sb.AppendLine("      <span>메이트: " + (Store.State.PetName ?? "스누피") + "</span>");
			sb.AppendLine("    </div>");
			sb.AppendLine("  </div>");

			sb.AppendLine("  <div class=\"summary-box\">");
			sb.AppendLine("    <div class=\"summary-card\"><div class=\"title\">포함된 프로젝트</div><div class=\"val\">" + targetProjects.Count + "개</div></div>");
			sb.AppendLine("    <div class=\"summary-card\"><div class=\"title\">기록된 Work Memo</div><div class=\"val\">" + totalMemos + "건</div></div>");
			sb.AppendLine("    <div class=\"summary-card\"><div class=\"title\">조회 기준</div><div class=\"val\">" + (isAll ? "전체 프로젝트 모아보기" : "개별 프로젝트 상세") + "</div></div>");
			sb.AppendLine("  </div>");

			foreach (var proj in targetProjects) {
				var entries = GetEntriesByProject(d, proj.Id, proj.Name);
				sb.AppendLine("  <div class=\"project-section\">");
				sb.AppendLine("    <div class=\"project-header\">");
				sb.AppendLine("      <div>");
				sb.AppendLine("        <div class=\"project-title\">프로젝트: " + System.Security.SecurityElement.Escape(proj.Name) + "</div>");
				sb.AppendLine("        <div class=\"project-desc\">" + System.Security.SecurityElement.Escape(String.IsNullOrEmpty(proj.Description) ? "등록된 설명 없음" : proj.Description) + "</div>");
				sb.AppendLine("      </div>");
				sb.AppendLine("      <div><span class=\"badge badge-progress\">" + System.Security.SecurityElement.Escape(proj.Status ?? "진행 중") + "</span> (" + entries.Count + "건)</div>");
				sb.AppendLine("    </div>");

				if (entries.Count == 0) {
					sb.AppendLine("    <div class=\"empty-memo\">이 프로젝트로 등록된 Work Memo 기록이 없습니다.</div>");
				} else {
					sb.AppendLine("    <table>");
					sb.AppendLine("      <thead>");
					sb.AppendLine("        <tr><th style=\"width: 140px;\">작성일자</th><th style=\"width: 110px;\">진행상태</th><th>Work Memo 내용</th></tr>");
					sb.AppendLine("      </thead>");
					sb.AppendLine("      <tbody>");
					foreach (var item in entries) {
						string prog = item.Item2.Progress ?? "진행 중";
						string badgeClass = prog.Contains("완료") ? "badge-done" : "badge-progress";
						string content = String.IsNullOrEmpty(item.Item2.Content) ? "(내용 없음)" : System.Security.SecurityElement.Escape(item.Item2.Content);
						sb.AppendLine("        <tr>");
						sb.AppendLine("          <td><strong>" + item.Item1 + "</strong></td>");
						sb.AppendLine("          <td><span class=\"badge " + badgeClass + "\">" + System.Security.SecurityElement.Escape(prog) + "</span></td>");
						sb.AppendLine("          <td><div class=\"memo-text\">" + content + "</div></td>");
						sb.AppendLine("        </tr>");
					}
					sb.AppendLine("      </tbody>");
					sb.AppendLine("    </table>");
				}
				sb.AppendLine("  </div>");
			}

			sb.AppendLine("  <div class=\"footer\">DeskBuddy Work Diary System · 맞춤형 업무일지 & 프로젝트 관리</div>");
			sb.AppendLine("</div>");
			sb.AppendLine("</body>");
			sb.AppendLine("</html>");
			return sb.ToString();
		}

		public static void Tests() {
			var d = new Data();
			Normalize(d);
			if (d.Projects.Count != 1 || d.Projects[0].Name != "기본 프로젝트") throw new Exception("Diary normalize projects");
			var diary = GetOrCreateDiary(d, "2026-10-07");
			if (diary.Date != "2026-10-07") throw new Exception("Diary create");
			diary.WorkMemo = "테스트 메모";
			diary.ProjectEntries.Add(new DiaryProjectEntry { ProjectId = d.Projects[0].Id, ProjectName = d.Projects[0].Name, Content = "프로젝트 메모 내용", Progress = "80%" });
			var projectLogs = GetEntriesByProject(d, d.Projects[0].Id, d.Projects[0].Name);
			if (projectLogs.Count != 1 || projectLogs[0].Item2.Progress != "80%") throw new Exception("Diary project query");

			var task = new TaskItem { Title = "오늘 회의", Due = DateTime.Today.AddHours(14), Done = true };
			d.Tasks.Add(task);
			List<TaskItem> todayTasks; int total, completed, percent;
			GetTodoStats(d, DateTime.Today, out todayTasks, out total, out completed, out percent);
			if (total != 1 || completed != 1 || percent != 100) throw new Exception("Diary todo stats");

			string csv = GenerateCsv(d, "all");
			if (!csv.Contains("기본 프로젝트") || !csv.Contains("프로젝트 메모 내용")) throw new Exception("Diary CSV export");

			string html = GenerateHtmlReport(d, "all");
			if (!html.Contains("DeskBuddy 업무일지") || !html.Contains("프로젝트 메모 내용")) throw new Exception("Diary HTML report export");

			string singleCsv = GenerateCsv(d, d.Projects[0].Id);
			if (!singleCsv.Contains("기본 프로젝트")) throw new Exception("Diary single CSV export");

			var testDiary = GetOrCreateDiary(d, "2026-10-08");
			int coinsBefore = d.Coins;
			int xpBefore = Companions.Experience(d);
			if (!RewardDiary(testDiary, d, null, null)) throw new Exception("Diary reward initial");
			if (d.Coins != coinsBefore + 25 || Companions.Experience(d) != xpBefore + 15) throw new Exception("Diary reward amounts");
			if (RewardDiary(testDiary, d, null, null)) throw new Exception("Diary duplicate reward prevented");

			string json = new JavaScriptSerializer().Serialize(d);
			var restored = new JavaScriptSerializer().Deserialize<Data>(json);
			Normalize(restored);
			if (restored.Diaries.Count != 2 || restored.Diaries[0].WorkMemo != "테스트 메모") throw new Exception("Diary JSON serialization");
		}
	}

	public static class DiaryTests {
		public static void Run() {
			DiaryCore.Tests();
			MonthlyReports.Tests();
		}
	}

	public class DiaryExportForm : Form {
		ProjectItem currentProject;
		RadioButton rbCurrentProject, rbAllProjects;
		RadioButton rbFormatExcel, rbFormatPdf;

		public DiaryExportForm(ProjectItem currentProject, string defaultFormat = "excel") {
			this.currentProject = currentProject;
			Text = "업무일지 메모 내보내기";
			FormBorderStyle = FormBorderStyle.None;
			ClientSize = new Size(580, 430);
			StartPosition = FormStartPosition.CenterParent;
			BackColor = Theme.Bg;
			Font = Theme.Font(10);
			DoubleBuffered = true;

			BuildLayout(defaultFormat);
		}

		protected override void OnPaint(PaintEventArgs e) {
			base.OnPaint(e);
			using (var p = new Pen(Theme.Line, 4)) {
				e.Graphics.DrawRectangle(p, 2, 2, Width - 4, Height - 4);
			}
		}

		void BuildLayout(string defaultFormat) {
			Controls.Clear();
			Controls.Add(Theme.Label("EXPORT / SAVE REPORT", 20, 16, 400, 20, 9, Theme.Muted));
			Controls.Add(Theme.Label("업무일지 & Work Memo 내보내기", 20, 36, 480, 32, 14));
			Controls.Add(Theme.Button("X", 522, 14, 38, 38, (s, e) => Close()));

			var card = new PixelPanel { Location = new Point(20, 78), Size = new Size(540, 276) };
			Controls.Add(card);

			// Section 1: Scope Panel (Group 1)
			var pnlScope = new Panel {
				Location = new Point(14, 10),
				Size = new Size(512, 100),
				BackColor = Color.Transparent
			};
			card.Controls.Add(pnlScope);

			pnlScope.Controls.Add(Theme.Label("[1] 내보낼 범위 선택 (프로젝트 모아보기)", 0, 0, 480, 22, 10));

			string curProjName = currentProject != null ? currentProject.Name : "기본 프로젝트";
			string curProjId = currentProject != null ? currentProject.Id : "";
			int curMemoCount = DiaryCore.GetEntriesByProject(Store.State, curProjId, curProjName).Count;

			rbCurrentProject = new RadioButton {
				Text = "현재 선택된 프로젝트 (" + curProjName + ") · " + curMemoCount + "건의 메모",
				Location = new Point(6, 28),
				Size = new Size(498, 28),
				Font = Theme.Font(9),
				ForeColor = Theme.Ink,
				Checked = true,
				Cursor = Cursors.Hand
			};
			pnlScope.Controls.Add(rbCurrentProject);

			int allMemoCount = 0;
			foreach (var p in Store.State.Projects) {
				allMemoCount += DiaryCore.GetEntriesByProject(Store.State, p.Id, p.Name).Count;
			}

			rbAllProjects = new RadioButton {
				Text = "전체 프로젝트 모아서 내보내기 (" + Store.State.Projects.Count + "개 프로젝트 · 총 " + allMemoCount + "건)",
				Location = new Point(6, 60),
				Size = new Size(498, 28),
				Font = Theme.Font(9),
				ForeColor = Theme.Ink,
				Cursor = Cursors.Hand
			};
			pnlScope.Controls.Add(rbAllProjects);

			// Section 2: Format Panel (Group 2)
			var pnlFormat = new Panel {
				Location = new Point(14, 116),
				Size = new Size(512, 150),
				BackColor = Color.Transparent
			};
			card.Controls.Add(pnlFormat);

			pnlFormat.Controls.Add(Theme.Label("[2] 내보낼 파일 형식", 0, 0, 480, 22, 10));

			rbFormatExcel = new RadioButton {
				Text = "엑셀 스프레드시트 (.csv / Excel에서 바로 열람 및 편집)",
				Location = new Point(6, 26),
				Size = new Size(498, 28),
				Font = Theme.Font(9),
				ForeColor = Theme.Ink,
				Checked = defaultFormat == "excel",
				Cursor = Cursors.Hand
			};
			pnlFormat.Controls.Add(rbFormatExcel);

			rbFormatPdf = new RadioButton {
				Text = "PDF 보고서 (.html / 브라우저에서 인쇄 및 1클릭 PDF 저장)",
				Location = new Point(6, 58),
				Size = new Size(498, 28),
				Font = Theme.Font(9),
				ForeColor = Theme.Ink,
				Checked = defaultFormat == "pdf",
				Cursor = Cursors.Hand
			};
			pnlFormat.Controls.Add(rbFormatPdf);

			var lblHint = Theme.Label("※ PDF 보고서는 깔끔한 A4 규격으로 브라우저 인쇄(PDF 저장)가 지원됩니다.", 6, 94, 498, 40, 9, Theme.Muted);
			pnlFormat.Controls.Add(lblHint);

			var btnDo = Theme.Button("파일 생성 및 열기", 20, 368, 380, 44, (s, e) => DoExport(), true);
			Controls.Add(btnDo);

			var btnCancel = Theme.Button("취소", 414, 368, 146, 44, (s, e) => Close());
			Controls.Add(btnCancel);
		}

		void DoExport() {
			bool isAll = rbAllProjects.Checked;
			bool isExcel = rbFormatExcel.Checked;
			string projId = isAll ? "all" : (currentProject != null ? currentProject.Id : "");
			string projName = isAll ? "전체프로젝트" : (currentProject != null ? currentProject.Name : "프로젝트");
			string safeProjName = String.Join("_", projName.Split(Path.GetInvalidFileNameChars()));
			string dateTag = AppClock.Now.ToString("yyyyMMdd");

			using (var sfd = new SaveFileDialog()) {
				if (isExcel) {
					sfd.Title = "업무일지 엑셀(CSV) 파일 저장";
					sfd.Filter = "CSV 파일 (Excel 호환)|*.csv|모든 파일|*.*";
					sfd.FileName = "DeskBuddy_메모_" + safeProjName + "_" + dateTag + ".csv";
				} else {
					sfd.Title = "업무일지 PDF 보고서 파일 저장";
					sfd.Filter = "HTML 보고서 / PDF (*.html)|*.html|모든 파일|*.*";
					sfd.FileName = "DeskBuddy_보고서_" + safeProjName + "_" + dateTag + ".html";
				}

				if (sfd.ShowDialog(this) == DialogResult.OK) {
					string path = sfd.FileName;
					try {
						if (isExcel) {
							string csv = DiaryCore.GenerateCsv(Store.State, projId);
							File.WriteAllText(path, csv, new UTF8Encoding(true));
						} else {
							string html = DiaryCore.GenerateHtmlReport(Store.State, projId);
							File.WriteAllText(path, html, Encoding.UTF8);
						}

						try {
							System.Diagnostics.Process.Start(path);
						} catch {}

						GameAlert.Show("내보내기가 성공적으로 완료되었습니다!\n파일 위치: " + path + "\n(연결된 프로그램에서 자동으로 열립니다)", "내보내기 완료");
						Close();
					} catch (Exception ex) {
						GameAlert.Show("파일 저장 중 오류가 발생했습니다.\n" + ex.Message, "저장 오류");
					}
				}
			}
		}
	}

	public partial class WorkDiaryPanel : Panel {
		MainForm parent;
		PetForm pet;
		DateTime selectedDate = AppClock.Now.Date;
		string mode = "daily"; // daily, byProject, manageProjects, monthly
		string selectedProjectId = "";

		Panel mainContent;
		Button tabDaily, tabByProject, tabManage, tabMonthly;

		const int CARD_WIDTH = 668;

		public WorkDiaryPanel(MainForm parent, PetForm pet) {
			this.parent = parent;
			this.pet = pet;
			Location = new Point(0, 0);
			Size = new Size(700, 1400);
			BackColor = Theme.Bg;
			DoubleBuffered = true;

			DiaryCore.Normalize(Store.State);
			if (Store.State.Projects.Count > 0 && String.IsNullOrEmpty(selectedProjectId)) {
				selectedProjectId = Store.State.Projects[0].Id;
			}

			BuildLayout();
		}

		void BuildLayout() {
			Controls.Clear();
			int y = 0;

			// Header
			var lblEn = Theme.Label("WORK DIARY / SAVE FILE", 14, y, CARD_WIDTH, 20, 9, Theme.Muted);
			Controls.Add(lblEn);
			y += 22;

			var lblTitle = Theme.Label("업무일지 & 프로젝트 Work Memo", 14, y, CARD_WIDTH, 34, 16);
			Controls.Add(lblTitle);
			y += 36;

			var lblSub = Theme.Label("오늘의 To-Do 달성률과 프로젝트별 업무 메모를 한 곳에서 기록하세요. [일지 작성 보상: +25 G · +15 EXP]", 14, y, CARD_WIDTH, 22, 9, Theme.Muted);
			Controls.Add(lblSub);
			y += 28;

			// Mode Tabs
			tabDaily = Theme.Button("일자별 일지 작성", 14, y, 160, 38, (s, e) => { mode = "daily"; RefreshMode(); });
			tabByProject = Theme.Button("프로젝트 모아보기", 180, y, 168, 38, (s, e) => { mode = "byProject"; RefreshMode(); });
			tabManage = Theme.Button("나의 프로젝트 관리", 354, y, 164, 38, (s, e) => { mode = "manageProjects"; RefreshMode(); });
			tabMonthly = Theme.Button("월간 리포트 AI", 524, y, 158, 38, (s, e) => { mode = "monthly"; RefreshMode(); });
			tabDaily.ForeColor = Theme.Ink;
			tabByProject.ForeColor = Theme.Ink;
			tabManage.ForeColor = Theme.Ink;
			tabMonthly.ForeColor = Theme.Ink;
			Controls.Add(tabDaily);
			Controls.Add(tabByProject);
			Controls.Add(tabManage);
			Controls.Add(tabMonthly);
			y += 48;

			// Main Content container
			mainContent = new Panel { Location = new Point(14, y), Size = new Size(CARD_WIDTH, 1300), BackColor = Theme.Bg };
			Controls.Add(mainContent);

			RefreshMode();
		}

		void RefreshMode() {
			Store.Save();

			tabDaily.BackColor = mode == "daily" ? Theme.Gold : Theme.Cream;
			tabDaily.ForeColor = Theme.Ink;
			tabDaily.Invalidate();

			tabByProject.BackColor = mode == "byProject" ? Theme.Gold : Theme.Cream;
			tabByProject.ForeColor = Theme.Ink;
			tabByProject.Invalidate();

			tabManage.BackColor = mode == "manageProjects" ? Theme.Gold : Theme.Cream;
			tabManage.ForeColor = Theme.Ink;
			tabManage.Invalidate();

			tabMonthly.BackColor = mode == "monthly" ? Theme.Gold : Theme.Cream;
			tabMonthly.ForeColor = Theme.Ink;
			tabMonthly.Invalidate();

			mainContent.Controls.Clear();
			if (mode == "daily") RenderDailyView();
			else if (mode == "byProject") RenderByProjectView();
			else if (mode == "monthly") RenderMonthlyReportView();
			else RenderManageProjectsView();
		}

		void OpenExportDialog(ProjectItem proj, string defaultFormat = "excel") {
			using (var dlg = new DiaryExportForm(proj, defaultFormat)) {
				dlg.ShowDialog(parent);
			}
		}

		#region Mode 1: Daily View (일자별 작성)

		void RenderDailyView() {
			mainContent.Controls.Clear();
			int y = 0;

			string dateStr = selectedDate.ToString("yyyy-MM-dd");
			var diary = DiaryCore.GetOrCreateDiary(Store.State, dateStr);

			// 1. Date Selector Bar
			var dateBar = new PixelPanel { Location = new Point(0, y), Size = new Size(CARD_WIDTH, 46), BackColor = Theme.Cream };
			mainContent.Controls.Add(dateBar);

			var btnPrev = Theme.Button("◀ 이전 날", 8, 5, 96, 36, (s, e) => {
				Store.Save();
				selectedDate = selectedDate.AddDays(-1);
				RenderDailyView();
			});
			dateBar.Controls.Add(btnPrev);

			string displayDate = selectedDate.ToString("yyyy-MM-dd (ddd)") + (selectedDate == AppClock.Now.Date ? " [오늘]" : "");
			var lblDate = Theme.Label(displayDate, 110, 8, 360, 28, 11);
			lblDate.TextAlign = ContentAlignment.MiddleCenter;
			dateBar.Controls.Add(lblDate);

			var btnNext = Theme.Button("다음 날 ▶", 478, 5, 96, 36, (s, e) => {
				Store.Save();
				selectedDate = selectedDate.AddDays(1);
				RenderDailyView();
			});
			dateBar.Controls.Add(btnNext);

			var btnToday = Theme.Button("오늘", 580, 5, 80, 36, (s, e) => {
				Store.Save();
				selectedDate = AppClock.Now.Date;
				RenderDailyView();
			}, true);
			dateBar.Controls.Add(btnToday);

			y += 56;

			// 1-1. Reward Status Notice Bar
			var rewardBar = new PixelPanel {
				Location = new Point(0, y),
				Size = new Size(CARD_WIDTH, 42),
				BackColor = diary.Rewarded ? Color.FromArgb(236, 246, 238) : Color.FromArgb(255, 248, 230)
			};
			mainContent.Controls.Add(rewardBar);

			string rewardMsg = diary.Rewarded
				? "[보상 획득 완료] 이 날짜의 업무일지 작성 보상 (+25 G 코인 · +15 EXP 경험치)을 받았습니다!"
				: "[일지 작성 보상] 오늘 업무일지(프로젝트 메모/일일 종합 메모)를 작성하면 +25 G와 +15 EXP가 지급됩니다!";
			var lblRewardStatus = Theme.Label(rewardMsg, 14, 9, 640, 24, 9, diary.Rewarded ? Theme.Purple : Color.FromArgb(180, 110, 0));
			rewardBar.Controls.Add(lblRewardStatus);

			y += 52;

			// 2. Section 1: 오늘 할 일 To-Do 달성률
			List<TaskItem> tasks;
			int total, completed, percent;
			DiaryCore.GetTodoStats(Store.State, selectedDate, out tasks, out total, out completed, out percent);

			int todoCardH = 105 + (tasks.Count == 0 ? 32 : tasks.Count * 42) + 52;
			var todoCard = new PixelPanel { Location = new Point(0, y), Size = new Size(CARD_WIDTH, todoCardH), BackColor = Theme.Cream };
			mainContent.Controls.Add(todoCard);

			var lblTodoTitle = Theme.Label("[오늘 할 일 To-Do 달성률]", 14, 12, 300, 24, 11);
			todoCard.Controls.Add(lblTodoTitle);

			string statStr = total == 0 ? "등록된 To-Do 없음 (0 / 0)" : completed + " / " + total + " 완료 (" + percent + "%)";
			var lblStat = Theme.Label(statStr, 330, 12, 324, 24, 10);
			lblStat.TextAlign = ContentAlignment.MiddleRight;
			lblStat.ForeColor = percent == 100 && total > 0 ? Color.FromArgb(180, 120, 0) : Theme.Purple;
			todoCard.Controls.Add(lblStat);

			// Progress bar
			var barOuter = new Panel { Location = new Point(14, 40), Size = new Size(640, 20), BackColor = Theme.Light };
			todoCard.Controls.Add(barOuter);
			int fillW = total == 0 ? 0 : Math.Max(2, (int)(634 * percent / 100.0));
			var barInner = new Panel { Location = new Point(3, 2), Size = new Size(fillW, 16), BackColor = percent == 100 ? Theme.Gold : Theme.Purple };
			barOuter.Controls.Add(barInner);

			// Task Checklist
			int taskY = 68;
			if (tasks.Count == 0) {
				var lblEmptyTask = Theme.Label("이 날짜에 등록된 To-Do가 없습니다. 아래에서 빠른 할 일을 등록해보세요.", 16, taskY, 630, 24, 9, Theme.Muted);
				todoCard.Controls.Add(lblEmptyTask);
				taskY += 30;
			} else {
				foreach (var t in tasks) {
					var tRef = t;
					var pnlTask = new Panel { Location = new Point(14, taskY), Size = new Size(640, 36), BackColor = tRef.Done ? Color.FromArgb(240, 245, 238) : Theme.Cream };
					todoCard.Controls.Add(pnlTask);

					var btnCheck = new PixelButton {
						Text = tRef.Done ? "완료" : "미완료",
						Location = new Point(2, 2),
						Size = new Size(80, 32),
						Font = Theme.Font(9),
						BackColor = tRef.Done ? Theme.Purple : Theme.Light,
						ForeColor = tRef.Done ? Theme.Cream : Theme.Ink
					};
					btnCheck.Click += (s, e) => {
						if (!tRef.Done) {
							Rules.Complete(tRef, Store.State);
							pet.Say("To-Do 완료! +20 G 획득!", false);
						} else {
							tRef.Done = false;
						}
						Store.Save();
						RenderDailyView();
					};
					pnlTask.Controls.Add(btnCheck);

					string titleText = tRef.Title + (tRef.Done ? " (완료)" : "");
					var lblTaskTitle = Theme.Label(titleText, 90, 6, 390, 22, 9, tRef.Done ? Theme.Muted : Theme.Ink);
					pnlTask.Controls.Add(lblTaskTitle);

					string dueText = Scheduler.Due(tRef).ToString("HH:mm") + " · " + tRef.Category;
					var lblTaskMeta = Theme.Label(dueText, 490, 6, 144, 22, 9, Theme.Muted);
					lblTaskMeta.TextAlign = ContentAlignment.MiddleRight;
					pnlTask.Controls.Add(lblTaskMeta);

					taskY += 40;
				}
			}

			// Quick Add Task Row
			var txtQuickTodo = Theme.Input("", 14, taskY + 6, 370);
			todoCard.Controls.Add(txtQuickTodo);

			var cmbQuickCat = Choice(new[] { "업무", "회의", "마감", "개인", "휴식" }, 392, taskY + 6, 100);
			todoCard.Controls.Add(cmbQuickCat);

			Action doAddQuick = () => {
				string title = txtQuickTodo.Text.Trim();
				if (String.IsNullOrEmpty(title)) {
					GameAlert.Show("할 일 내용을 입력해주세요.", "할 일 추가");
					return;
				}
				var newTask = new TaskItem {
					Title = title,
					Category = cmbQuickCat.SelectedItem != null ? cmbQuickCat.SelectedItem.ToString() : "업무",
					Due = selectedDate.Date.AddHours(9)
				};
				Scheduler.SetDue(newTask, newTask.Due);
				Store.State.Tasks.Add(newTask);
				Store.Save();
				txtQuickTodo.Clear();
				RenderDailyView();
			};

			txtQuickTodo.KeyDown += (s, e) => {
				if (e.KeyCode == Keys.Enter) {
					e.SuppressKeyPress = true;
					doAddQuick();
				}
			};

			var btnAddQuick = Theme.Button("+ 할 일 등록", 504, taskY + 5, 150, 36, (s, e) => doAddQuick(), true);
			todoCard.Controls.Add(btnAddQuick);

			y += todoCardH + 16;

			// 3. Section 2: 프로젝트별 Work Memo (프로젝트별 그룹 정렬!)
			var groups = diary.ProjectEntries.GroupBy(e => e.ProjectName).OrderBy(g => g.Key).ToList();
			int projCardH = 110;
			if (groups.Count == 0) {
				projCardH += 60;
			} else {
				foreach (var g in groups) {
					projCardH += 44 + g.Count() * 170; // Header + each memo card
				}
			}

			var projCard = new PixelPanel { Location = new Point(0, y), Size = new Size(CARD_WIDTH, projCardH), BackColor = Theme.Cream };
			mainContent.Controls.Add(projCard);

			var lblProjTitle = Theme.Label("[프로젝트별 Work Memo]", 14, 12, 320, 24, 11);
			projCard.Controls.Add(lblProjTitle);

			var lblProjSub = Theme.Label("프로젝트를 선택하고 [+ 메모 추가]를 누르면 프로젝트별로 정렬되어 기록됩니다.", 14, 36, 640, 20, 9, Theme.Muted);
			projCard.Controls.Add(lblProjSub);

			// Select Bar for adding new project work memo
			var projItems = Store.State.Projects.Select(p => p.Name).ToArray();
			if (projItems.Length == 0) projItems = new[] { "기본 프로젝트" };
			var cmbProjects = Choice(projItems, 14, 62, 190);
			projCard.Controls.Add(cmbProjects);

			var cmbProgress = Choice(new[] { "진행 중", "30%", "50%", "80%", "완료", "이슈 해결", "검토 중" }, 212, 62, 105);
			projCard.Controls.Add(cmbProgress);

			var btnAddMemo = Theme.Button("+ 메모 추가", 326, 61, 130, 36, (s, e) => {
				string pName = cmbProjects.SelectedItem != null ? cmbProjects.SelectedItem.ToString() : "기본 프로젝트";
				var pObj = Store.State.Projects.FirstOrDefault(x => x.Name == pName);
				string pId = pObj != null ? pObj.Id : "";
				string prog = cmbProgress.SelectedItem != null ? cmbProgress.SelectedItem.ToString() : "진행 중";

				diary.ProjectEntries.Add(new DiaryProjectEntry {
					ProjectId = pId,
					ProjectName = pName,
					Progress = prog,
					Content = ""
				});
				DiaryCore.RewardDiary(diary, Store.State, pet, parent);
				Store.Save();
				RenderDailyView();
			}, true);
			projCard.Controls.Add(btnAddMemo);

			var btnGoManage = Theme.Button("프로젝트 관리", 464, 61, 116, 36, (s, e) => {
				mode = "manageProjects";
				RefreshMode();
			});
			projCard.Controls.Add(btnGoManage);

			var btnExportDaily = Theme.Button("내보내기", 588, 61, 66, 36, (s, e) => {
				var pName = cmbProjects.SelectedItem != null ? cmbProjects.SelectedItem.ToString() : "";
				var pObj = Store.State.Projects.FirstOrDefault(x => x.Name == pName);
				OpenExportDialog(pObj, "excel");
			});
			projCard.Controls.Add(btnExportDaily);

			// Render project groups (Grouped by Project!)
			int groupY = 110;
			if (groups.Count == 0) {
				var lblNoEntries = Theme.Label("오늘 진행한 프로젝트를 선택하고 [+ 메모 추가] 버튼을 눌러 Work Memo를 기록하세요.", 16, groupY + 10, 638, 24, 9, Theme.Muted);
				projCard.Controls.Add(lblNoEntries);
			} else {
				foreach (var g in groups) {
					string pGroupName = g.Key;

					// Project Section Banner
					var pnlGroupHeader = new Panel { Location = new Point(14, groupY), Size = new Size(640, 36), BackColor = Color.FromArgb(238, 226, 204) };
					projCard.Controls.Add(pnlGroupHeader);

					var lblGroupTitle = Theme.Label("프로젝트: " + pGroupName + "  (" + g.Count() + "건의 메모)", 10, 6, 430, 24, 10, Theme.Purple);
					pnlGroupHeader.Controls.Add(lblGroupTitle);

					// Quick "+ 이 프로젝트 메모 추가" button
					var btnAddSameProj = Theme.Button("+ 이 프로젝트 메모 추가", 450, 4, 184, 28, (s, e) => {
						var pObj = Store.State.Projects.FirstOrDefault(x => x.Name == pGroupName);
						string pId = pObj != null ? pObj.Id : "";
						diary.ProjectEntries.Add(new DiaryProjectEntry {
							ProjectId = pId,
							ProjectName = pGroupName,
							Progress = "진행 중",
							Content = ""
						});
						DiaryCore.RewardDiary(diary, Store.State, pet, parent);
						Store.Save();
						RenderDailyView();
					});
					pnlGroupHeader.Controls.Add(btnAddSameProj);

					groupY += 42;

					// Render individual memo cards under this project
					foreach (var entry in g) {
						var entryRef = entry;
						var pnlEntry = new Panel { Location = new Point(14, groupY), Size = new Size(640, 160), BackColor = Color.FromArgb(250, 247, 239) };
						projCard.Controls.Add(pnlEntry);

						var lblStatusTag = Theme.Label("진행상태", 10, 7, 82, 24, 9, Theme.Muted);
						pnlEntry.Controls.Add(lblStatusTag);

						var cmbEntryProg = Choice(new[] { "진행 중", "30%", "50%", "80%", "완료", "이슈 해결", "검토 중" }, 94, 4, 115);
						cmbEntryProg.SelectedItem = entryRef.Progress;
						cmbEntryProg.SelectedIndexChanged += (s, e) => {
							entryRef.Progress = cmbEntryProg.SelectedItem != null ? cmbEntryProg.SelectedItem.ToString() : "진행 중";
							Store.Save();
						};
						pnlEntry.Controls.Add(cmbEntryProg);

						var btnDelEntry = Theme.Button("삭제", 554, 4, 76, 28, (s, e) => {
							if (GameAlert.Show("이 Work Memo를 삭제할까요?", "삭제 확인", MessageBoxButtons.YesNo) == DialogResult.Yes) {
								diary.ProjectEntries.Remove(entryRef);
								Store.Save();
								RenderDailyView();
							}
						});
						pnlEntry.Controls.Add(btnDelEntry);

						// Memo TextBox (Auto-save on TextChanged & Leave)
						var txtContent = new TextBox {
							Location = new Point(10, 38),
							Size = new Size(620, 112),
							Multiline = true,
							ScrollBars = ScrollBars.Vertical,
							Font = Theme.Font(9),
							BackColor = Theme.Cream,
							ForeColor = Theme.Ink,
							BorderStyle = BorderStyle.FixedSingle,
							Text = entryRef.Content ?? ""
						};
						pnlEntry.Controls.Add(txtContent);

						txtContent.TextChanged += (s, e) => {
							entryRef.Content = txtContent.Text;
						};

						txtContent.Leave += (s, e) => {
							entryRef.Content = txtContent.Text;
							if (!String.IsNullOrWhiteSpace(txtContent.Text)) {
								DiaryCore.RewardDiary(diary, Store.State, pet, parent);
							}
							Store.Save();
						};

						groupY += 168;
					}
				}
			}

			y += projCardH + 16;

			// 4. Section 3: 일일 종합 메모 (Daily General Memo - Auto-save)
			var memoCard = new PixelPanel { Location = new Point(0, y), Size = new Size(CARD_WIDTH, 146), BackColor = Theme.Cream };
			mainContent.Controls.Add(memoCard);

			var lblMemoTitle = Theme.Label("[일일 종합 메모] (자동 저장)", 14, 12, 380, 24, 11);
			memoCard.Controls.Add(lblMemoTitle);

			var txtMemo = new TextBox {
				Location = new Point(14, 42),
				Size = new Size(640, 92),
				Multiline = true,
				ScrollBars = ScrollBars.Vertical,
				Font = Theme.Font(9),
				BackColor = Theme.Cream,
				ForeColor = Theme.Ink,
				BorderStyle = BorderStyle.FixedSingle,
				Text = diary.WorkMemo ?? ""
			};
			memoCard.Controls.Add(txtMemo);

			txtMemo.TextChanged += (s, e) => {
				diary.WorkMemo = txtMemo.Text;
			};

			txtMemo.Leave += (s, e) => {
				diary.WorkMemo = txtMemo.Text;
				if (!String.IsNullOrWhiteSpace(txtMemo.Text)) {
					DiaryCore.RewardDiary(diary, Store.State, pet, parent);
				}
				Store.Save();
			};

			y += 160;
			mainContent.Height = y + 40;
			Height = Math.Max(Height, y + 200);
		}

		#endregion

		#region Mode 2: Project Aggregation View (프로젝트별 모아보기)

		void RenderByProjectView() {
			mainContent.Controls.Clear();
			int y = 0;

			var projectList = Store.State.Projects;
			var projectNames = projectList.Select(p => p.Name).ToArray();
			if (projectNames.Length == 0) projectNames = new[] { "기본 프로젝트" };

			var currentProj = projectList.FirstOrDefault(p => p.Id == selectedProjectId) ?? projectList.FirstOrDefault();
			if (currentProj != null) selectedProjectId = currentProj.Id;

			// 1. Project Selector & Export Control Card
			var topCard = new PixelPanel { Location = new Point(0, y), Size = new Size(CARD_WIDTH, 112), BackColor = Theme.Cream };
			mainContent.Controls.Add(topCard);

			topCard.Controls.Add(Theme.Label("모아볼 나의 프로젝트 선택:", 14, 8, 250, 22, 10));

			var cmbPick = Choice(projectNames, 14, 32, 296);
			if (currentProj != null) {
				cmbPick.SelectedItem = currentProj.Name;
			}
			cmbPick.SelectedIndexChanged += (s, e) => {
				var sel = projectList.FirstOrDefault(p => p.Name == cmbPick.SelectedItem.ToString());
				if (sel != null) selectedProjectId = sel.Id;
				RenderByProjectView();
			};
			topCard.Controls.Add(cmbPick);

			var btnManageShortcut = Theme.Button("+ 새 프로젝트", 320, 31, 150, 34, (s, e) => {
				mode = "manageProjects";
				RefreshMode();
			});
			topCard.Controls.Add(btnManageShortcut);

			var btnExport = Theme.Button("내보내기", 480, 31, 172, 34, (s, e) => OpenExportDialog(currentProj), true);
			topCard.Controls.Add(btnExport);

			string descText = currentProj != null ? "상태: " + currentProj.Status + " · " + (currentProj.Description ?? "설명 없음") : "";
			topCard.Controls.Add(Theme.Label(descText, 14, 72, 640, 22, 9, Theme.Muted));

			y += 124;

			// 2. Timeline of Entries for this project
			string targetId = currentProj != null ? currentProj.Id : "";
			string targetName = currentProj != null ? currentProj.Name : "";
			var history = DiaryCore.GetEntriesByProject(Store.State, targetId, targetName);

			var timelineHeader = Theme.Label("[" + targetName + "] Work Memo 히스토리 (" + history.Count + "건)", 0, y, CARD_WIDTH, 26, 11);
			mainContent.Controls.Add(timelineHeader);

			y += 34;

			if (history.Count == 0) {
				var emptyCard = new PixelPanel { Location = new Point(0, y), Size = new Size(CARD_WIDTH, 90), BackColor = Theme.Cream };
				emptyCard.Controls.Add(Theme.Label("이 프로젝트로 작성된 Work Memo가 아직 없습니다.\n[일자별 일지 작성]에서 이 프로젝트를 선택하고 [+ 메모 추가]를 눌러 작성해보세요!", 16, 20, 630, 48, 9, Theme.Muted));
				mainContent.Controls.Add(emptyCard);
				y += 100;
			} else {
				foreach (var item in history) {
					string dateStr = item.Item1;
					var entry = item.Item2;

					var entryCard = new PixelPanel { Location = new Point(0, y), Size = new Size(CARD_WIDTH, 130), BackColor = Theme.Cream };
					mainContent.Controls.Add(entryCard);

					DateTime dt;
					string dateDisplay = DateTime.TryParse(dateStr, out dt) ? dt.ToString("yyyy-MM-dd (ddd)") : dateStr;
					var lblEntryDate = Theme.Label("날짜: " + dateDisplay, 14, 8, 240, 22, 10, Theme.Purple);
					entryCard.Controls.Add(lblEntryDate);

					var lblProgBadge = Theme.Label("진행상태: " + (entry.Progress ?? "진행 중"), 260, 8, 180, 22, 9, Color.FromArgb(180, 120, 0));
					entryCard.Controls.Add(lblProgBadge);

					var btnJump = Theme.Button("이 날짜 일지 보기", 500, 6, 154, 30, (s, e) => {
						DateTime jumpDate;
						if (DateTime.TryParse(dateStr, out jumpDate)) {
							selectedDate = jumpDate.Date;
							mode = "daily";
							RefreshMode();
						}
					});
					entryCard.Controls.Add(btnJump);

					var txtViewMemo = new TextBox {
						Location = new Point(14, 36),
						Size = new Size(640, 84),
						Multiline = true,
						ReadOnly = true,
						ScrollBars = ScrollBars.Vertical,
						Font = Theme.Font(9),
						BackColor = Color.FromArgb(249, 245, 235),
						ForeColor = String.IsNullOrEmpty(entry.Content) ? Theme.Muted : Theme.Ink,
						BorderStyle = BorderStyle.FixedSingle,
						Text = String.IsNullOrEmpty(entry.Content) ? "(작성된 세부 내용 없음)" : entry.Content
					};
					entryCard.Controls.Add(txtViewMemo);

					y += 140;
				}
			}

			mainContent.Height = y + 40;
			Height = Math.Max(Height, y + 200);
		}

		#endregion

		#region Mode 3: Project Management View (나의 프로젝트 등록 및 관리)

		void RenderManageProjectsView() {
			mainContent.Controls.Clear();
			int y = 0;

			// 1. Add New Project Card
			var addCard = new PixelPanel { Location = new Point(0, y), Size = new Size(CARD_WIDTH, 162), BackColor = Theme.Cream };
			mainContent.Controls.Add(addCard);

			addCard.Controls.Add(Theme.Label("[새로운 프로젝트 등록]", 14, 10, 300, 24, 11));
			addCard.Controls.Add(Theme.Label("업무일지에서 select 박스로 빠르게 불러올 수 있도록 나의 프로젝트를 등록하세요.", 14, 32, 640, 20, 9, Theme.Muted));

			addCard.Controls.Add(Theme.Label("프로젝트명", 14, 58, 90, 20, 9));
			var txtName = Theme.Input("", 14, 80, 250);
			addCard.Controls.Add(txtName);

			addCard.Controls.Add(Theme.Label("설명 / 목표", 276, 58, 140, 20, 9));
			var txtDesc = Theme.Input("", 276, 80, 230);
			addCard.Controls.Add(txtDesc);

			addCard.Controls.Add(Theme.Label("상태", 518, 58, 80, 20, 9));
			var cmbStatus = Choice(new[] { "진행 중", "완료", "대기" }, 518, 80, 136);
			addCard.Controls.Add(cmbStatus);

			var btnCreate = Theme.Button("프로젝트 등록하기", 14, 116, 640, 36, (s, e) => {
				string pName = txtName.Text.Trim();
				if (String.IsNullOrEmpty(pName)) {
					GameAlert.Show("프로젝트명을 입력해주세요.", "프로젝트 등록");
					return;
				}
				if (Store.State.Projects.Any(p => p.Name == pName)) {
					GameAlert.Show("동일한 이름의 프로젝트가 이미 존재합니다.", "프로젝트 등록");
					return;
				}
				Store.State.Projects.Add(new ProjectItem {
					Name = pName,
					Description = txtDesc.Text.Trim(),
					Status = cmbStatus.SelectedItem != null ? cmbStatus.SelectedItem.ToString() : "진행 중",
					CreatedDate = AppClock.Now.ToString("yyyy-MM-dd")
				});
				Store.Save();
				txtName.Clear();
				txtDesc.Clear();
				GameAlert.Show("프로젝트 '" + pName + "' 등록 완료!\n업무일지에서 바로 선택하여 Work Memo를 작성할 수 있습니다.", "등록 완료");
				RenderManageProjectsView();
			}, true);
			addCard.Controls.Add(btnCreate);

			y += 174;

			// 2. Existing Projects List
			var lblListTitle = Theme.Label("나의 등록된 프로젝트 목록 (" + Store.State.Projects.Count + "개)", 0, y, CARD_WIDTH, 24, 11);
			mainContent.Controls.Add(lblListTitle);
			y += 28;

			if (Store.State.Projects.Count == 0) {
				var emptyCard = new PixelPanel { Location = new Point(0, y), Size = new Size(CARD_WIDTH, 75), BackColor = Theme.Cream };
				emptyCard.Controls.Add(Theme.Label("등록된 프로젝트가 없습니다. 위에서 새 프로젝트를 등록해보세요.", 16, 24, 630, 24, 9, Theme.Muted));
				mainContent.Controls.Add(emptyCard);
				y += 85;
			} else {
				foreach (var proj in Store.State.Projects) {
					var pRef = proj;
					var pCard = new PixelPanel { Location = new Point(0, y), Size = new Size(CARD_WIDTH, 78), BackColor = Theme.Cream };
					mainContent.Controls.Add(pCard);

					var lblPName = Theme.Label("프로젝트: " + pRef.Name, 14, 10, 230, 24, 10, Theme.Purple);
					pCard.Controls.Add(lblPName);

					var lblPStatus = Theme.Label("[" + pRef.Status + "]", 250, 10, 80, 24, 9, Color.FromArgb(180, 120, 0));
					pCard.Controls.Add(lblPStatus);

					var lblPDesc = Theme.Label(String.IsNullOrEmpty(pRef.Description) ? "(설명 없음)" : pRef.Description, 14, 38, 450, 24, 9, Theme.Muted);
					pCard.Controls.Add(lblPDesc);

					var btnSelectView = Theme.Button("모아보기", 476, 10, 88, 30, (s, e) => {
						selectedProjectId = pRef.Id;
						mode = "byProject";
						RefreshMode();
					});
					pCard.Controls.Add(btnSelectView);

					var btnDelete = Theme.Button("삭제", 572, 10, 82, 30, (s, e) => {
						if (Store.State.Projects.Count <= 1) {
							GameAlert.Show("최소 1개의 프로젝트는 유지되어야 합니다.", "삭제 불가");
							return;
						}
						if (GameAlert.Show("프로젝트 '" + pRef.Name + "'을(를) 삭제할까요?\n(기존에 작성된 업무일지 기록은 보존됩니다)", "프로젝트 삭제", MessageBoxButtons.YesNo) == DialogResult.Yes) {
							Store.State.Projects.Remove(pRef);
							Store.Save();
							RenderManageProjectsView();
						}
					});
					pCard.Controls.Add(btnDelete);

					y += 88;
				}
			}

			mainContent.Height = y + 40;
			Height = Math.Max(Height, y + 200);
		}

		#endregion

		ComboBox Choice(string[] items, int x, int y, int w) {
			var c = new ComboBox {
				Location = new Point(x, y),
				Size = new Size(w, 30),
				Font = Theme.Font(9),
				BackColor = Theme.Cream,
				ForeColor = Theme.Ink,
				DropDownStyle = ComboBoxStyle.DropDownList
			};
			foreach (var s in items) c.Items.Add(s);
			if (c.Items.Count > 0) c.SelectedIndex = 0;
			return c;
		}
	}
}
