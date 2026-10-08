using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace DeskBuddy {

	public class KpiItem {
		public string Id = Guid.NewGuid().ToString("N");
		public string Name = "";
		public string Target = "";
		public string Description = "";
	}

	public class KpiResult {
		public string KpiId = "";
		public string KpiName = "";
		public string Target = "";
		public string Actual = "";
		public string Achievement = "";
		public string Evidence = "";
	}

	public class MonthlyReport {
		public string Month = ""; // yyyy-MM
		public string Summary = "";
		public string Highlights = "";
		public List<KpiResult> Kpis = new List<KpiResult>();
		public string GeneratedBy = "";
		public string GeneratedAt = "";
	}

	// Provider-neutral JSON completion over raw HTTP (the build uses csc/.NET Framework 4 without NuGet, so no vendor SDKs).
	public static class AiAssistant {
		public static readonly string[] Providers = { "Claude", "OpenAI", "Gemini" };
		static readonly byte[] Entropy = Encoding.UTF8.GetBytes("DeskBuddy.AiKey.v1");
		// Models that accept Anthropic's server-side refusal fallback ("fallbacks": "default").
		static readonly string[] ClaudeFallbackModels = { "claude-fable-5-1", "claude-opus-5-5", "claude-opus-5", "claude-sonnet-5-5" };

		public static string DefaultModel(string provider) {
			switch (provider) {
				case "OpenAI": return "gpt-5";
				case "Gemini": return "gemini-2.5-flash";
				default: return "claude-opus-5-5";
			}
		}

		public static string Model(Data d, string provider) {
			string model;
			if (d.AiModels != null && d.AiModels.TryGetValue(provider, out model) && !String.IsNullOrWhiteSpace(model)) return model.Trim();
			return DefaultModel(provider);
		}

		static string KeyPath(string provider) { return Path.Combine(Store.Root, "ai-" + provider.ToLowerInvariant() + ".secret"); }
		public static string Key(string provider) {
			try { return Encoding.UTF8.GetString(ProtectedData.Unprotect(File.ReadAllBytes(KeyPath(provider)), Entropy, DataProtectionScope.CurrentUser)); } catch { return ""; }
		}
		public static bool HasKey(string provider) { return Key(provider).Length > 0; }
		public static void SaveKey(string provider, string key) {
			key = (key ?? "").Trim();
			if (key.Length == 0) throw new Exception("API 키를 입력해주세요.");
			Directory.CreateDirectory(Store.Root);
			File.WriteAllBytes(KeyPath(provider), ProtectedData.Protect(Encoding.UTF8.GetBytes(key), Entropy, DataProtectionScope.CurrentUser));
		}
		public static void DeleteKey(string provider) { if (File.Exists(KeyPath(provider))) File.Delete(KeyPath(provider)); }

		static JavaScriptSerializer Json() { return new JavaScriptSerializer { MaxJsonLength = Int32.MaxValue }; }

		// Sends one system + user prompt and returns the model's JSON text. Runs on a worker thread.
		public static string CompleteJson(string provider, string model, string key, string system, string user, Dictionary<string, object> schema) {
			if (!Regex.IsMatch(model ?? "", "^[A-Za-z0-9._:-]+$")) throw new Exception("모델 이름이 올바르지 않아요: " + model);
			var headers = new Dictionary<string, string>();
			if (provider == "OpenAI") {
				headers["Authorization"] = "Bearer " + key;
				var body = new Dictionary<string, object> {
					{ "model", model },
					{ "messages", new object[] {
						new Dictionary<string, object> { { "role", "system" }, { "content", system } },
						new Dictionary<string, object> { { "role", "user" }, { "content", user } } } },
					{ "response_format", new Dictionary<string, object> {
						{ "type", "json_schema" },
						{ "json_schema", new Dictionary<string, object> { { "name", "monthly_report" }, { "strict", true }, { "schema", schema } } } } }
				};
				return ReadOpenAI(Post("https://api.openai.com/v1/chat/completions", headers, body));
			}
			if (provider == "Gemini") {
				headers["x-goog-api-key"] = key;
				var body = new Dictionary<string, object> {
					{ "systemInstruction", new Dictionary<string, object> { { "parts", new object[] { new Dictionary<string, object> { { "text", system } } } } } },
					{ "contents", new object[] { new Dictionary<string, object> {
						{ "role", "user" },
						{ "parts", new object[] { new Dictionary<string, object> { { "text", user } } } } } } },
					{ "generationConfig", new Dictionary<string, object> { { "responseMimeType", "application/json" }, { "responseJsonSchema", schema } } }
				};
				return ReadGemini(Post("https://generativelanguage.googleapis.com/v1beta/models/" + model + ":generateContent", headers, body));
			}
			headers["x-api-key"] = key;
			headers["anthropic-version"] = "2023-06-01";
			var claude = new Dictionary<string, object> {
				{ "model", model },
				{ "max_tokens", 16000 },
				{ "system", system },
				{ "messages", new object[] { new Dictionary<string, object> { { "role", "user" }, { "content", user } } } },
				{ "output_config", new Dictionary<string, object> {
					{ "effort", "medium" },
					{ "format", new Dictionary<string, object> { { "type", "json_schema" }, { "schema", schema } } } } }
			};
			if (ClaudeFallbackModels.Contains(model)) {
				headers["anthropic-beta"] = "server-side-fallback-2026-07-01";
				claude["fallbacks"] = "default";
			}
			return ReadClaude(Post("https://api.anthropic.com/v1/messages", headers, claude));
		}

		static string Post(string url, Dictionary<string, string> headers, object body) {
			ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
			var request = (HttpWebRequest)WebRequest.Create(url);
			request.Method = "POST";
			request.ContentType = "application/json";
			request.Timeout = 180000;
			request.ReadWriteTimeout = 180000;
			foreach (var h in headers) request.Headers[h.Key] = h.Value;
			byte[] payload = Encoding.UTF8.GetBytes(Json().Serialize(body));
			try {
				using (var stream = request.GetRequestStream()) stream.Write(payload, 0, payload.Length);
				using (var response = request.GetResponse())
				using (var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8)) return reader.ReadToEnd();
			} catch (WebException ex) {
				var response = ex.Response as HttpWebResponse;
				if (response == null) throw new Exception("AI 서버에 연결하지 못했어요. 인터넷 연결을 확인해주세요.");
				string text;
				using (response)
				using (var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8)) text = reader.ReadToEnd();
				throw new Exception(DescribeError((int)response.StatusCode, text));
			}
		}

		public static string DescribeError(int status, string body) {
			string detail = "";
			try { detail = Str(Get(Get(Json().DeserializeObject(body), "error"), "message")); } catch { }
			string head;
			if (status == 401 || status == 403) head = "API 키가 올바르지 않거나 권한이 없어요.";
			else if (status == 404) head = "모델을 찾지 못했어요. 모델 이름을 확인해주세요.";
			else if (status == 429) head = "요청 한도 또는 크레딧이 부족해요. 잠시 후 다시 시도해주세요.";
			else if (status >= 500) head = "AI 서버에 일시적인 문제가 있어요. 잠시 후 다시 시도해주세요.";
			else head = "AI 요청이 거절됐어요.";
			return head + " (HTTP " + status + ")" + (detail.Length > 0 ? "\n" + detail : "");
		}

		public static string ReadClaude(string response) {
			var root = Json().DeserializeObject(response);
			string stop = Str(Get(root, "stop_reason"));
			if (stop == "refusal") throw new Exception("AI가 이 요청에 응답하지 않았어요. 업무일지 내용을 확인해주세요.");
			if (stop == "max_tokens") throw new Exception("응답이 너무 길어 중간에 잘렸어요. 다시 시도해주세요.");
			var sb = new StringBuilder();
			foreach (var block in Arr(Get(root, "content")))
				if (Str(Get(block, "type")) == "text") sb.Append(Str(Get(block, "text")));
			return sb.ToString();
		}

		public static string ReadOpenAI(string response) {
			var choice = Arr(Get(Json().DeserializeObject(response), "choices")).FirstOrDefault();
			var message = Get(choice, "message");
			if (Str(Get(message, "refusal")).Length > 0) throw new Exception("AI가 이 요청에 응답하지 않았어요: " + Str(Get(message, "refusal")));
			if (Str(Get(choice, "finish_reason")) == "length") throw new Exception("응답이 너무 길어 중간에 잘렸어요. 다시 시도해주세요.");
			return Str(Get(message, "content"));
		}

		public static string ReadGemini(string response) {
			var candidate = Arr(Get(Json().DeserializeObject(response), "candidates")).FirstOrDefault();
			if (candidate == null) throw new Exception("AI가 이 요청에 응답하지 않았어요. 업무일지 내용을 확인해주세요.");
			string finish = Str(Get(candidate, "finishReason"));
			if (finish == "MAX_TOKENS") throw new Exception("응답이 너무 길어 중간에 잘렸어요. 다시 시도해주세요.");
			if (finish == "SAFETY" || finish == "RECITATION") throw new Exception("AI가 이 요청에 응답하지 않았어요. (" + finish + ")");
			var sb = new StringBuilder();
			foreach (var part in Arr(Get(Get(candidate, "content"), "parts"))) sb.Append(Str(Get(part, "text")));
			return sb.ToString();
		}

		public static object Get(object node, string key) {
			var dict = node as IDictionary<string, object>;
			if (dict == null) return null;
			object value;
			if (dict.TryGetValue(key, out value)) return value;
			var match = dict.Keys.FirstOrDefault(k => String.Equals(k, key, StringComparison.OrdinalIgnoreCase));
			return match == null ? null : dict[match];
		}
		public static IEnumerable<object> Arr(object node) {
			if (node == null || node is string) return new object[0];
			var list = node as IEnumerable;
			return list == null ? new object[0] : list.Cast<object>();
		}
		public static string Str(object node) {
			if (node == null) return "";
			if (node is string) return (string)node;
			// Models sometimes return a list (or object) where a sentence was asked for; flatten it to lines.
			var dict = node as IDictionary<string, object>;
			if (dict != null) return String.Join("\n", dict.Values.Select(Str).Where(s => s.Length > 0).ToArray());
			if (node is IEnumerable) return String.Join("\n", Arr(node).Select(Str).Where(s => s.Length > 0).ToArray());
			return Convert.ToString(node, CultureInfo.InvariantCulture);
		}
	}

	public static class MonthlyReports {
		public const string SystemPrompt =
			"당신은 직장인의 업무일지를 바탕으로 월간 업무 보고서를 작성하는 비서입니다. 회사에 제출할 보고서이므로 업무일지에 적힌 사실만 사용하고, 일지에 없는 수치나 성과를 지어내지 마세요.\n" +
			"- summary: 이번 달 업무 전반을 3~6개의 개조식 문장으로 요약합니다. 문장 하나를 배열 항목 하나로 쓰고 '~함', '~완료' 같은 보고서 문체를 씁니다.\n" +
			"- highlights: 업무일지에 등장한 프로젝트마다 이번 달 주요 성과와 진행 상황을 1~3문장으로 정리합니다.\n" +
			"- kpis: 제공된 KPI 항목마다 정확히 하나씩 작성하고 kpiId는 그대로 옮깁니다. actual에는 일지에서 확인되는 실적, achievement에는 목표 대비 달성률(예: 80%) 또는 계산할 수 없으면 '판단 불가', evidence에는 근거가 된 날짜와 내용을 적습니다. 관련 기록이 없으면 actual은 '확인된 실적 없음', evidence는 '업무일지에서 관련 기록을 찾지 못함'으로 적습니다.\n" +
			"모든 내용은 한국어로 작성합니다.";

		public static void Normalize(Data d) {
			if (d.Kpis == null) d.Kpis = new List<KpiItem>();
			if (d.MonthlyReports == null) d.MonthlyReports = new List<MonthlyReport>();
			if (d.AiModels == null) d.AiModels = new Dictionary<string, string>();
			if (!AiAssistant.Providers.Contains(d.AiProvider)) d.AiProvider = "Claude";
			foreach (var k in d.Kpis) if (String.IsNullOrEmpty(k.Id)) k.Id = Guid.NewGuid().ToString("N");
			foreach (var r in d.MonthlyReports) if (r.Kpis == null) r.Kpis = new List<KpiResult>();
		}

		public static MonthlyReport GetOrCreate(Data d, string month) {
			Normalize(d);
			var report = d.MonthlyReports.FirstOrDefault(r => r.Month == month);
			if (report == null) {
				report = new MonthlyReport { Month = month };
				d.MonthlyReports.Add(report);
			}
			return report;
		}

		public static KpiResult ResultFor(MonthlyReport report, KpiItem kpi) {
			var result = report.Kpis.FirstOrDefault(r => r.KpiId == kpi.Id);
			if (result == null) {
				result = new KpiResult { KpiId = kpi.Id };
				report.Kpis.Add(result);
			}
			result.KpiName = kpi.Name;
			result.Target = kpi.Target;
			return result;
		}

		public static bool HasContent(MonthlyReport r) {
			return !String.IsNullOrWhiteSpace(r.Summary) || !String.IsNullOrWhiteSpace(r.Highlights) ||
				r.Kpis.Any(k => !String.IsNullOrWhiteSpace(k.Actual) || !String.IsNullOrWhiteSpace(k.Evidence));
		}

		static bool Written(WorkDiaryItem diary) {
			return diary != null && (!String.IsNullOrWhiteSpace(diary.WorkMemo) || diary.ProjectEntries.Any(e => !String.IsNullOrWhiteSpace(e.Content)));
		}

		public static int WrittenDays(Data d, DateTime month) {
			string prefix = month.ToString("yyyy-MM") + "-";
			return d.Diaries.Count(x => x.Date != null && x.Date.StartsWith(prefix) && Written(x));
		}

		// Renders the month's diaries, to-dos, projects and KPI definitions as the user prompt.
		public static string BuildPrompt(Data d, DateTime month) {
			DiaryCore.Normalize(d);
			Normalize(d);
			var start = new DateTime(month.Year, month.Month, 1);
			var sb = new StringBuilder();
			sb.AppendLine("대상 월: " + start.ToString("yyyy-MM"));
			sb.AppendLine();
			sb.AppendLine("[등록된 프로젝트]");
			foreach (var p in d.Projects) sb.AppendLine("- " + p.Name + " (" + p.Status + ")" + (String.IsNullOrWhiteSpace(p.Description) ? "" : ": " + p.Description));
			sb.AppendLine();
			sb.AppendLine("[KPI 항목]");
			if (d.Kpis.Count == 0) sb.AppendLine("(등록된 KPI 없음 - kpis는 빈 배열로 작성)");
			foreach (var k in d.Kpis) sb.AppendLine("- kpiId=" + k.Id + " | 지표명: " + k.Name + " | 목표: " + (String.IsNullOrWhiteSpace(k.Target) ? "(미정)" : k.Target) + (String.IsNullOrWhiteSpace(k.Description) ? "" : " | 설명: " + k.Description));
			sb.AppendLine();
			sb.AppendLine("[업무일지]");
			int days = 0;
			for (var day = start; day.Month == start.Month; day = day.AddDays(1)) {
				var diary = d.Diaries.FirstOrDefault(x => x.Date == day.ToString("yyyy-MM-dd"));
				List<TaskItem> tasks; int total, completed, percent;
				DiaryCore.GetTodoStats(d, day, out tasks, out total, out completed, out percent);
				if (!Written(diary) && total == 0) continue;
				days++;
				sb.AppendLine("## " + day.ToString("yyyy-MM-dd (ddd)", CultureInfo.GetCultureInfo("ko-KR")));
				if (total > 0) {
					sb.AppendLine("To-Do: " + completed + "/" + total + " 완료 (" + percent + "%)");
					foreach (var t in tasks) sb.AppendLine("  - [" + (t.Done ? "완료" : "미완료") + "] " + t.Title + " (" + t.Category + ")");
				}
				if (diary != null) {
					foreach (var e in diary.ProjectEntries.Where(x => !String.IsNullOrWhiteSpace(x.Content)))
						sb.AppendLine("프로젝트 메모 - " + e.ProjectName + " [" + e.Progress + "]: " + e.Content.Trim());
					if (!String.IsNullOrWhiteSpace(diary.WorkMemo)) sb.AppendLine("일일 종합 메모: " + diary.WorkMemo.Trim());
				}
				sb.AppendLine();
			}
			if (days == 0) throw new Exception(start.ToString("yyyy년 M월") + "에 작성된 업무일지나 To-Do가 없어요.\n일지를 먼저 작성해주세요.");
			return sb.ToString();
		}

		static Dictionary<string, object> Text(string description) {
			return new Dictionary<string, object> { { "type", "string" }, { "description", description } };
		}
		static Dictionary<string, object> Object(Dictionary<string, object> properties) {
			return new Dictionary<string, object> {
				{ "type", "object" },
				{ "properties", properties },
				{ "required", properties.Keys.ToArray() },
				{ "additionalProperties", false }
			};
		}

		public static Dictionary<string, object> Schema() {
			return Object(new Dictionary<string, object> {
				{ "summary", new Dictionary<string, object> { { "type", "array" }, { "description", "월간 업무 내역 요약. 개조식 문장 하나가 항목 하나" }, { "items", new Dictionary<string, object> { { "type", "string" } } } } },
				{ "highlights", new Dictionary<string, object> { { "type", "array" }, { "items", Object(new Dictionary<string, object> {
					{ "project", Text("프로젝트명") },
					{ "achievement", Text("이번 달 주요 성과와 진행 상황") } }) } } },
				{ "kpis", new Dictionary<string, object> { { "type", "array" }, { "items", Object(new Dictionary<string, object> {
					{ "kpiId", Text("입력으로 받은 kpiId 그대로") },
					{ "actual", Text("실적") },
					{ "achievement", Text("목표 대비 달성률 또는 '판단 불가'") },
					{ "evidence", Text("근거가 된 날짜와 업무일지 내용") } }) } } }
			});
		}

		// Turns the model's JSON into a report shaped by the current KPI list; does not touch saved state.
		public static MonthlyReport Parse(string json, Data d, string month) {
			json = json ?? "";
			int open = json.IndexOf('{'), close = json.LastIndexOf('}');
			if (open < 0 || close <= open) throw new Exception("AI 응답을 해석하지 못했어요. 다시 시도해주세요.");
			object root;
			try { root = new JavaScriptSerializer { MaxJsonLength = Int32.MaxValue }.DeserializeObject(json.Substring(open, close - open + 1)); } catch { throw new Exception("AI 응답을 해석하지 못했어요. 다시 시도해주세요."); }

			var report = new MonthlyReport { Month = month };
			report.Summary = NewLines(AiAssistant.Str(AiAssistant.Get(root, "summary")).Trim());
			report.Highlights = String.Join(Environment.NewLine, AiAssistant.Arr(AiAssistant.Get(root, "highlights"))
				.Select(h => "• " + AiAssistant.Str(AiAssistant.Get(h, "project")).Trim() + ": " + NewLines(AiAssistant.Str(AiAssistant.Get(h, "achievement")).Trim()))
				.ToArray());
			var answers = AiAssistant.Arr(AiAssistant.Get(root, "kpis")).ToList();
			foreach (var kpi in d.Kpis) {
				var result = ResultFor(report, kpi);
				var answer = answers.FirstOrDefault(a => AiAssistant.Str(AiAssistant.Get(a, "kpiId")) == kpi.Id);
				if (answer == null) continue;
				result.Actual = NewLines(AiAssistant.Str(AiAssistant.Get(answer, "actual")).Trim());
				result.Achievement = AiAssistant.Str(AiAssistant.Get(answer, "achievement")).Trim();
				result.Evidence = NewLines(AiAssistant.Str(AiAssistant.Get(answer, "evidence")).Trim());
			}
			return report;
		}

		// TextBox needs CRLF to show line breaks.
		static string NewLines(string text) { return Regex.Replace(text, "\r?\n", Environment.NewLine); }

		public static void Apply(Data d, MonthlyReport generated) {
			var report = GetOrCreate(d, generated.Month);
			report.Summary = generated.Summary;
			report.Highlights = generated.Highlights;
			foreach (var g in generated.Kpis) {
				var existing = report.Kpis.FirstOrDefault(r => r.KpiId == g.KpiId);
				if (existing != null) report.Kpis.Remove(existing);
				report.Kpis.Add(g);
			}
			report.GeneratedBy = generated.GeneratedBy;
			report.GeneratedAt = generated.GeneratedAt;
		}

		public static void Tests() {
			var d = new Data();
			DiaryCore.Normalize(d);
			Normalize(d);
			if (d.AiProvider != "Claude" || AiAssistant.Model(d, "Claude") != "claude-opus-5-5") throw new Exception("AI provider defaults");
			d.AiModels["Gemini"] = "gemini-test";
			if (AiAssistant.Model(d, "Gemini") != "gemini-test") throw new Exception("AI model override");

			var kpi = new KpiItem { Name = "고객 문의 처리", Target = "월 30건" };
			d.Kpis.Add(kpi);
			var diary = DiaryCore.GetOrCreateDiary(d, "2026-10-07");
			diary.WorkMemo = "고객 문의 12건 처리";
			diary.ProjectEntries.Add(new DiaryProjectEntry { ProjectId = d.Projects[0].Id, ProjectName = d.Projects[0].Name, Content = "API 연동 완료", Progress = "완료" });
			var september = DiaryCore.GetOrCreateDiary(d, "2026-09-30");
			september.WorkMemo = "지난달 기록";

			string prompt = BuildPrompt(d, new DateTime(2026, 10, 1));
			if (!prompt.Contains(kpi.Id) || !prompt.Contains("고객 문의 12건 처리") || !prompt.Contains("API 연동 완료") || prompt.Contains("지난달 기록")) throw new Exception("Monthly prompt content");
			if (WrittenDays(d, new DateTime(2026, 10, 1)) != 1) throw new Exception("Monthly written days");
			bool rejected = false;
			try { BuildPrompt(d, new DateTime(2026, 11, 1)); } catch { rejected = true; }
			if (!rejected) throw new Exception("Empty month must be rejected");

			string answer = "{\"summary\":[\"문의 처리\",\"API 연동 완료함\"],\"highlights\":[{\"project\":\"기본 프로젝트\",\"achievement\":\"API 연동 완료\"}],\"kpis\":[{\"kpiId\":\"" + kpi.Id + "\",\"actual\":\"12건\",\"achievement\":\"40%\",\"evidence\":\"10/07 고객 문의 12건\"},{\"kpiId\":\"unknown\",\"actual\":\"x\",\"achievement\":\"x\",\"evidence\":\"x\"}]}";
			var parsed = Parse("```json\n" + answer + "\n```", d, "2026-10");
			if (parsed.Summary != "문의 처리" + Environment.NewLine + "API 연동 완료함" || !parsed.Highlights.Contains("기본 프로젝트: API 연동 완료")) throw new Exception("Monthly parse summary");
			if (parsed.Kpis.Count != 1 || parsed.Kpis[0].Actual != "12건" || parsed.Kpis[0].Achievement != "40%" || parsed.Kpis[0].KpiName != "고객 문의 처리") throw new Exception("Monthly parse KPI");

			var listed = Parse("{\"Summary\":[\"문의 처리함\",\"배포 완료함\"],\"highlights\":[],\"kpis\":[]}", d, "2026-10");
			if (listed.Summary != "문의 처리함" + Environment.NewLine + "배포 완료함") throw new Exception("Monthly parse summary list");

			var report = GetOrCreate(d, "2026-10");
			report.Summary = "직접 쓴 내용";
			if (!HasContent(report)) throw new Exception("Monthly content detection");
			Apply(d, parsed);
			if (d.MonthlyReports.Count != 1 || GetOrCreate(d, "2026-10").Kpis[0].Evidence != "10/07 고객 문의 12건") throw new Exception("Monthly apply");

			if (AiAssistant.ReadClaude("{\"stop_reason\":\"end_turn\",\"content\":[{\"type\":\"thinking\",\"thinking\":\"\"},{\"type\":\"text\",\"text\":\"{}\"}]}") != "{}") throw new Exception("Claude response");
			rejected = false;
			try { AiAssistant.ReadClaude("{\"stop_reason\":\"refusal\",\"content\":[]}"); } catch { rejected = true; }
			if (!rejected) throw new Exception("Claude refusal");
			if (AiAssistant.ReadOpenAI("{\"choices\":[{\"finish_reason\":\"stop\",\"message\":{\"content\":\"{}\",\"refusal\":null}}]}") != "{}") throw new Exception("OpenAI response");
			if (AiAssistant.ReadGemini("{\"candidates\":[{\"finishReason\":\"STOP\",\"content\":{\"parts\":[{\"text\":\"{\"},{\"text\":\"}\"}]}}]}") != "{}") throw new Exception("Gemini response");
			if (!AiAssistant.DescribeError(401, "{\"error\":{\"message\":\"invalid x-api-key\"}}").Contains("invalid x-api-key")) throw new Exception("AI error message");

			string json = new JavaScriptSerializer().Serialize(d);
			var restored = new JavaScriptSerializer().Deserialize<Data>(json);
			Normalize(restored);
			if (restored.Kpis.Count != 1 || restored.MonthlyReports[0].Kpis[0].Actual != "12건" || restored.AiModels["Gemini"] != "gemini-test") throw new Exception("Monthly JSON serialization");
		}
	}

	public partial class WorkDiaryPanel {
		DateTime reportMonth = new DateTime(AppClock.Now.Year, AppClock.Now.Month, 1);
		bool generating;

		#region Mode 4: Monthly Report (월간 리포트 · AI)

		void RenderMonthlyReportView() {
			mainContent.Controls.Clear();
			MonthlyReports.Normalize(Store.State);
			int y = 0;
			string provider = Store.State.AiProvider;

			// 1. AI connection
			var aiCard = new PixelPanel { Location = new Point(0, y), Size = new Size(CARD_WIDTH, 132), BackColor = Theme.Cream };
			mainContent.Controls.Add(aiCard);
			aiCard.Controls.Add(Theme.Label("[AI 연결 설정]", 14, 10, 300, 24, 11));

			aiCard.Controls.Add(Theme.Label("AI", 14, 44, 30, 24, 9));
			var cmbProvider = Choice(AiAssistant.Providers, 46, 42, 120);
			cmbProvider.SelectedItem = provider;
			cmbProvider.SelectedIndexChanged += (s, e) => {
				Store.State.AiProvider = cmbProvider.SelectedItem.ToString();
				Store.Save();
				RenderMonthlyReportView();
			};
			aiCard.Controls.Add(cmbProvider);

			aiCard.Controls.Add(Theme.Label("모델", 180, 44, 44, 24, 9));
			var txtModel = Theme.Input(AiAssistant.Model(Store.State, provider), 226, 42, 220);
			txtModel.Leave += (s, e) => {
				Store.State.AiModels[provider] = txtModel.Text.Trim();
				Store.Save();
			};
			aiCard.Controls.Add(txtModel);

			bool hasKey = AiAssistant.HasKey(provider);
			aiCard.Controls.Add(Theme.Label(hasKey ? "● 키 저장됨" : "○ 키 없음", 458, 44, 196, 24, 9, hasKey ? Theme.Purple : Color.FromArgb(180, 110, 0)));

			aiCard.Controls.Add(Theme.Label("API 키", 14, 86, 60, 24, 9));
			var txtKey = Theme.Input("", 76, 84, 318);
			txtKey.UseSystemPasswordChar = true;
			aiCard.Controls.Add(txtKey);
			aiCard.Controls.Add(Theme.Button("키 저장", 402, 82, 120, 34, (s, e) => {
				try {
					AiAssistant.SaveKey(provider, txtKey.Text);
					GameAlert.Show(provider + " API 키를 이 PC에 암호화해서 저장했어요.", "AI 연결");
					RenderMonthlyReportView();
				} catch (Exception ex) { GameAlert.Show(ex.Message, "AI 연결"); }
			}, true));
			aiCard.Controls.Add(Theme.Button("키 삭제", 530, 82, 124, 34, (s, e) => {
				if (!hasKey) return;
				if (GameAlert.Show(provider + " API 키를 삭제할까요?", "AI 연결", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
				AiAssistant.DeleteKey(provider);
				RenderMonthlyReportView();
			}));
			y += 144;

			// 2. KPI definitions
			int kpiCardH = 132 + Math.Max(1, Store.State.Kpis.Count) * 40;
			var kpiCard = new PixelPanel { Location = new Point(0, y), Size = new Size(CARD_WIDTH, kpiCardH), BackColor = Theme.Cream };
			mainContent.Controls.Add(kpiCard);
			kpiCard.Controls.Add(Theme.Label("[KPI 항목 정의]", 14, 10, 300, 24, 11));
			kpiCard.Controls.Add(Theme.Label("평가받는 KPI를 등록하면 AI가 매달 실적·달성률·근거를 채워줘요.", 14, 34, 640, 20, 9, Theme.Muted));
			kpiCard.Controls.Add(Theme.Label("지표명", 14, 58, 120, 20, 9));
			kpiCard.Controls.Add(Theme.Label("목표", 220, 58, 120, 20, 9));
			kpiCard.Controls.Add(Theme.Label("설명 (선택)", 372, 58, 160, 20, 9));
			var txtKpiName = Theme.Input("", 14, 80, 198);
			var txtKpiTarget = Theme.Input("", 220, 80, 144);
			var txtKpiDesc = Theme.Input("", 372, 80, 180);
			kpiCard.Controls.Add(txtKpiName);
			kpiCard.Controls.Add(txtKpiTarget);
			kpiCard.Controls.Add(txtKpiDesc);
			kpiCard.Controls.Add(Theme.Button("+ 추가", 560, 78, 94, 34, (s, e) => {
				string name = txtKpiName.Text.Trim();
				if (name.Length == 0) { GameAlert.Show("지표명을 입력해주세요.", "KPI 추가"); return; }
				Store.State.Kpis.Add(new KpiItem { Name = name, Target = txtKpiTarget.Text.Trim(), Description = txtKpiDesc.Text.Trim() });
				Store.Save();
				RenderMonthlyReportView();
			}, true));

			int rowY = 124;
			if (Store.State.Kpis.Count == 0) {
				kpiCard.Controls.Add(Theme.Label("등록된 KPI가 없어요. 예) 고객 문의 처리 / 월 30건", 16, rowY + 6, 630, 24, 9, Theme.Muted));
			}
			foreach (var kpi in Store.State.Kpis.ToList()) {
				var kRef = kpi;
				string line = kRef.Name + "  ·  목표: " + (String.IsNullOrWhiteSpace(kRef.Target) ? "-" : kRef.Target) + (String.IsNullOrWhiteSpace(kRef.Description) ? "" : "  ·  " + kRef.Description);
				kpiCard.Controls.Add(Theme.Label(line, 16, rowY + 6, 540, 24, 9));
				kpiCard.Controls.Add(Theme.Button("삭제", 572, rowY + 2, 82, 30, (s, e) => {
					if (GameAlert.Show("KPI '" + kRef.Name + "'을(를) 삭제할까요?\n(이미 작성된 월간 리포트 기록은 보존됩니다)", "KPI 삭제", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
					Store.State.Kpis.Remove(kRef);
					Store.Save();
					RenderMonthlyReportView();
				}));
				rowY += 40;
			}
			y += kpiCardH + 12;

			// 3. Month selector + AI generation
			string month = reportMonth.ToString("yyyy-MM");
			var report = MonthlyReports.GetOrCreate(Store.State, month);
			var monthBar = new PixelPanel { Location = new Point(0, y), Size = new Size(CARD_WIDTH, 120), BackColor = Theme.Cream };
			mainContent.Controls.Add(monthBar);
			monthBar.Controls.Add(Theme.Button("◀ 이전 달", 8, 5, 110, 36, (s, e) => { Store.Save(); reportMonth = reportMonth.AddMonths(-1); RenderMonthlyReportView(); }));
			var lblMonth = Theme.Label(reportMonth.ToString("yyyy년 M월") + "  ·  일지 " + MonthlyReports.WrittenDays(Store.State, reportMonth) + "일 작성", 124, 8, 420, 28, 11);
			lblMonth.TextAlign = ContentAlignment.MiddleCenter;
			monthBar.Controls.Add(lblMonth);
			monthBar.Controls.Add(Theme.Button("다음 달 ▶", 550, 5, 110, 36, (s, e) => { Store.Save(); reportMonth = reportMonth.AddMonths(1); RenderMonthlyReportView(); }));
			var lblStatus = Theme.Label(String.IsNullOrEmpty(report.GeneratedAt) ? "업무일지를 바탕으로 아래 양식을 AI가 채워요. 결과는 직접 수정할 수 있어요." : "마지막 AI 작성: " + report.GeneratedAt + "  ·  " + report.GeneratedBy, 14, 88, 640, 22, 9, Theme.Muted);
			monthBar.Controls.Add(lblStatus);
			Button btnGenerate = null;
			btnGenerate = Theme.Button(generating ? "AI가 작성 중..." : "AI로 자동 작성 (" + provider + ")", 14, 46, 640, 38, (s, e) => GenerateMonthlyReport(btnGenerate, lblStatus), true);
			btnGenerate.Enabled = !generating;
			monthBar.Controls.Add(btnGenerate);
			y += 132;

			// 4. Editable report form
			y = ReportTextCard("[월간 업무 내역 요약]", report.Summary, y, 150, text => report.Summary = text);
			y = ReportTextCard("[프로젝트별 주요 성과]", report.Highlights, y, 130, text => report.Highlights = text);

			int kpiResultH = 50 + Math.Max(1, Store.State.Kpis.Count) * 168;
			var resultCard = new PixelPanel { Location = new Point(0, y), Size = new Size(CARD_WIDTH, kpiResultH), BackColor = Theme.Cream };
			mainContent.Controls.Add(resultCard);
			resultCard.Controls.Add(Theme.Label("[KPI 성과]", 14, 12, 300, 24, 11));
			int resultY = 44;
			if (Store.State.Kpis.Count == 0) {
				resultCard.Controls.Add(Theme.Label("위에서 KPI 항목을 먼저 등록해주세요.", 16, resultY + 10, 630, 24, 9, Theme.Muted));
			}
			foreach (var kpi in Store.State.Kpis) {
				var result = MonthlyReports.ResultFor(report, kpi);
				var row = new Panel { Location = new Point(14, resultY), Size = new Size(640, 160), BackColor = Color.FromArgb(250, 247, 239) };
				resultCard.Controls.Add(row);
				row.Controls.Add(Theme.Label(kpi.Name + "  (목표: " + (String.IsNullOrWhiteSpace(kpi.Target) ? "-" : kpi.Target) + ")", 10, 6, 620, 24, 10, Theme.Purple));
				row.Controls.Add(Theme.Label("실적", 10, 40, 40, 22, 9));
				row.Controls.Add(BoundBox(result.Actual, 52, 36, 380, 28, false, text => result.Actual = text));
				row.Controls.Add(Theme.Label("달성률", 444, 40, 60, 22, 9));
				row.Controls.Add(BoundBox(result.Achievement, 506, 36, 124, 28, false, text => result.Achievement = text));
				row.Controls.Add(Theme.Label("근거", 10, 76, 40, 22, 9));
				row.Controls.Add(BoundBox(result.Evidence, 52, 72, 578, 80, true, text => result.Evidence = text));
				resultY += 168;
			}
			y += kpiResultH + 16;

			mainContent.Height = y + 40;
			Height = Math.Max(Height, y + 200);
		}

		int ReportTextCard(string title, string text, int y, int height, Action<string> update) {
			var card = new PixelPanel { Location = new Point(0, y), Size = new Size(CARD_WIDTH, height + 56), BackColor = Theme.Cream };
			mainContent.Controls.Add(card);
			card.Controls.Add(Theme.Label(title + " (자동 저장)", 14, 12, 400, 24, 11));
			card.Controls.Add(BoundBox(text, 14, 42, 640, height, true, update));
			return y + height + 68;
		}

		TextBox BoundBox(string text, int x, int y, int w, int h, bool multiline, Action<string> update) {
			var box = new TextBox {
				Location = new Point(x, y),
				Size = new Size(w, h),
				Multiline = multiline,
				ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None,
				Font = Theme.Font(9),
				BackColor = Theme.Cream,
				ForeColor = Theme.Ink,
				BorderStyle = BorderStyle.FixedSingle,
				Text = text ?? ""
			};
			box.TextChanged += (s, e) => update(box.Text);
			box.Leave += (s, e) => { update(box.Text); Store.Save(); };
			return box;
		}

		async void GenerateMonthlyReport(Button button, Label status) {
			if (generating) return;
			Store.Save();
			string provider = Store.State.AiProvider;
			string key = AiAssistant.Key(provider);
			if (key.Length == 0) { GameAlert.Show(provider + " API 키를 먼저 저장해주세요.", "AI 자동 작성"); return; }
			string month = reportMonth.ToString("yyyy-MM");
			if (Store.State.Kpis.Count == 0 && GameAlert.Show("등록된 KPI가 없어요. 업무 요약만 작성할까요?", "AI 자동 작성", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
			if (MonthlyReports.HasContent(MonthlyReports.GetOrCreate(Store.State, month)) &&
				GameAlert.Show("이미 작성된 내용이 있어요. AI 결과로 덮어쓸까요?", "AI 자동 작성", MessageBoxButtons.YesNo) != DialogResult.Yes) return;

			string prompt;
			try { prompt = MonthlyReports.BuildPrompt(Store.State, reportMonth); } catch (Exception ex) { GameAlert.Show(ex.Message, "AI 자동 작성"); return; }
			string model = AiAssistant.Model(Store.State, provider);
			var schema = MonthlyReports.Schema();

			generating = true;
			button.Enabled = false;
			button.Text = "AI가 작성 중...";
			status.Text = "AI가 업무일지를 읽고 리포트를 작성하고 있어요. (최대 1~2분)";
			bool done = false;
			try {
				string json = await Task.Run(() => AiAssistant.CompleteJson(provider, model, key, MonthlyReports.SystemPrompt, prompt, schema));
				var generated = MonthlyReports.Parse(json, Store.State, month);
				generated.GeneratedBy = provider + " · " + model;
				generated.GeneratedAt = AppClock.Now.ToString("yyyy-MM-dd HH:mm");
				MonthlyReports.Apply(Store.State, generated);
				Store.Save();
				done = true;
			} catch (Exception ex) {
				GameAlert.Show("AI 자동 작성에 실패했어요.\n" + ex.Message, "AI 자동 작성");
			} finally {
				generating = false;
			}
			if (done && pet != null) pet.Say(reportMonth.ToString("M월") + " 리포트를 작성했어요! 내용을 확인해주세요.", false);
			if (!IsDisposed && mode == "monthly") RenderMonthlyReportView();
		}

		#endregion
	}
}
