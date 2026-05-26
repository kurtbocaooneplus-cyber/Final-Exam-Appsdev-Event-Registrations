using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace EventRegistrationSystem
{

    //EventRegistration

    public class EventRegistration
    {
        public string RecordId { get; set; }
        public string EventName { get; set; }
        public string AttendeeName { get; set; }
        public string Email { get; set; }
        public string ContactNumber { get; set; }
        public string EventDate { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsActive { get; set; }
        public string Checksum { get; set; }

        private const char SEP = '|';

        public string ToCsvLine()
        {
            return string.Join(SEP.ToString(), new string[]
            {
                Escape(RecordId),
                Escape(EventName),
                Escape(AttendeeName),
                Escape(Email),
                Escape(ContactNumber),
                Escape(EventDate),
                Escape(Status),
                CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                IsActive ? "1" : "0",
                Escape(Checksum)
            });
        }

        public static EventRegistration FromCsvLine(string line)
        {
            string[] parts = line.Split(SEP);
            if (parts.Length < 11)
                throw new FormatException("Record line has fewer fields than expected: " + line);

            return new EventRegistration
            {
                RecordId = Unescape(parts[0]),
                EventName = Unescape(parts[1]),
                AttendeeName = Unescape(parts[2]),
                Email = Unescape(parts[3]),
                ContactNumber = Unescape(parts[4]),
                EventDate = Unescape(parts[5]),
                Status = Unescape(parts[6]),
                CreatedAt = DateTime.Parse(parts[7]),
                UpdatedAt = DateTime.Parse(parts[8]),
                IsActive = parts[9] == "1",
                Checksum = Unescape(parts[10])
            };
        }

        private static string Escape(string value)
        {
            if (value == null) return string.Empty;
            return value.Replace("\\", "\\\\").Replace("|", "\\p");
        }

        private static string Unescape(string value)
        {
            if (value == null) return string.Empty;
            return value.Replace("\\p", "|").Replace("\\\\", "\\");
        }
    }


    //ChecksumHelper

    public static class ChecksumHelper
    {
        public static string Compute(EventRegistration r)
        {
            string raw = string.Concat(
                r.RecordId, "|",
                r.EventName, "|",
                r.AttendeeName, "|",
                r.Email, "|",
                r.ContactNumber, "|",
                r.EventDate, "|",
                r.Status, "|",
                r.IsActive.ToString());

            using (MD5 md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(raw));
                StringBuilder sb = new StringBuilder();
                foreach (byte b in hash)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        public static bool Verify(EventRegistration r)
        {
            return Compute(r) == r.Checksum;
        }
    }


    //ConsoleHelper

    public static class ConsoleHelper
    {
        public static void WriteError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        public static void WriteSuccess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        public static void WriteWarning(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        public static void WriteInfo(string message)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        public static void WriteHeader(string title)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.WriteLine("  " + title.ToUpper() + "  ");
            Console.ResetColor();
            Console.WriteLine(new string('=', 60));
        }

        public static void WriteDivider()
        {
            Console.WriteLine(new string('-', 60));
        }

        public static void PressAnyKey()
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("Press any key to return to the menu...");
            Console.ResetColor();
            Console.ReadKey(true);
        }

        public static void PrintRecord(EventRegistration r)
        {
            WriteDivider();
            Console.WriteLine("  Record ID   : " + r.RecordId);
            Console.WriteLine("  Event       : " + r.EventName);
            Console.WriteLine("  Event Date  : " + r.EventDate);
            Console.WriteLine("  Attendee    : " + r.AttendeeName);
            Console.WriteLine("  Email       : " + r.Email);
            Console.WriteLine("  Contact No. : " + r.ContactNumber);

            Console.Write("  Status      : ");
            switch (r.Status.ToLower())
            {
                case "confirmed": Console.ForegroundColor = ConsoleColor.Green; break;
                case "pending": Console.ForegroundColor = ConsoleColor.Yellow; break;
                case "cancelled": Console.ForegroundColor = ConsoleColor.Red; break;
            }
            Console.WriteLine(r.Status);
            Console.ResetColor();

            Console.WriteLine("  Created At  : " + r.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            Console.WriteLine("  Updated At  : " + r.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            Console.WriteLine("  Active      : " + (r.IsActive ? "Yes" : "No"));
            Console.WriteLine("  Checksum    : " + r.Checksum);
        }
    }


    //AuditLogger

    public class AuditLogger
    {
        private readonly string _logFilePath;

        public AuditLogger(string logFilePath)
        {
            _logFilePath = logFilePath;
        }

        public void Log(string action, string details)
        {
            string entry = string.Format("[{0}] | {1,-12} | {2}",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), action, details);
            try { File.AppendAllText(_logFilePath, entry + Environment.NewLine); }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError("WARNING: Could not write to audit log. " + ex.Message);
            }
        }

        public void LogAdd(string recordId, string summary)
        {
            Log("ADD", string.Format("RecordId={0} | {1}", recordId, summary));
        }

        public void LogUpdate(string recordId, string summary)
        {
            Log("UPDATE", string.Format("RecordId={0} | {1}", recordId, summary));
        }

        public void LogSoftDelete(string recordId) { Log("SOFT-DELETE", string.Format("RecordId={0}", recordId)); }
        public void LogHardDelete(string recordId) { Log("HARD-DELETE", string.Format("RecordId={0}", recordId)); }
        public void LogRead(string details) { Log("READ", details); }
        public void LogReport(string reportName) { Log("REPORT", reportName); }
        public void LogError(string details) { Log("ERROR", details); }
        public void LogSystem(string details) { Log("SYSTEM", details); }
    }


    //StorageInitializer

    public static class StorageInitializer
    {
        public static void Initialise(string dataFolder, string dataFilePath,
                                       string auditFilePath, string reportsFolder)
        {
            try
            {
                if (!Directory.Exists(dataFolder))
                {
                    Directory.CreateDirectory(dataFolder);
                    Console.WriteLine("[INIT] Created data folder: " + dataFolder);
                }

                if (!Directory.Exists(reportsFolder))
                {
                    Directory.CreateDirectory(reportsFolder);
                    Console.WriteLine("[INIT] Created reports folder: " + reportsFolder);
                }

                if (!File.Exists(dataFilePath))
                {
                    File.WriteAllText(dataFilePath, string.Empty);
                    Console.WriteLine("[INIT] Created data file: " + dataFilePath);
                }

                if (!File.Exists(auditFilePath))
                {
                    File.WriteAllText(auditFilePath,
                        "# Event Registration System - Audit Log" + Environment.NewLine +
                        "# Created: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") +
                        Environment.NewLine);
                    Console.WriteLine("[INIT] Created audit log: " + auditFilePath);
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[FATAL] Storage initialisation failed: " + ex.Message);
                Console.ResetColor();
                throw;
            }
        }
    }


    //Validator

    public static class Validator
    {
        public static string ValidateEventName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "Event name cannot be empty.";
            if (value.Trim().Length < 3) return "Event name must be at least 3 characters.";
            if (value.Trim().Length > 100) return "Event name cannot exceed 100 characters.";
            return null;
        }

        public static string ValidateAttendeeName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "Attendee name cannot be empty.";
            if (value.Trim().Length < 2) return "Attendee name must be at least 2 characters.";
            if (value.Trim().Length > 80) return "Attendee name cannot exceed 80 characters.";
            if (!Regex.IsMatch(value.Trim(), @"^[A-Za-z\s\-']+$"))
                return "Attendee name may only contain letters, spaces, hyphens, and apostrophes.";
            return null;
        }

        public static string ValidateEmail(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "Email address cannot be empty.";
            if (!Regex.IsMatch(value.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                return "Email address is not in a valid format (e.g. name@example.com).";
            if (value.Trim().Length > 120) return "Email address cannot exceed 120 characters.";
            return null;
        }

        public static string ValidateContactNumber(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "Contact number cannot be empty.";
            string digits = Regex.Replace(value, @"[\s\-\(\)\+]", "");
            if (!Regex.IsMatch(digits, @"^\d{7,15}$")) return "Contact number must contain 7-15 digits.";
            return null;
        }

        public static string ValidateEventDate(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "Event date cannot be empty.";
            DateTime dt;
            if (!DateTime.TryParseExact(value.Trim(), "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out dt))
                return "Event date must be in yyyy-MM-dd format (e.g. 2025-12-31).";
            if (dt.Date < DateTime.Today)
                return "Event date cannot be in the past. Please enter today or a future date.";
            return null;
        }

        public static string ValidateStatus(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "Status cannot be empty.";
            string[] allowed = { "Confirmed", "Pending", "Cancelled" };
            foreach (string s in allowed)
                if (string.Equals(s, value.Trim(), StringComparison.OrdinalIgnoreCase))
                    return null;
            return "Status must be one of: Confirmed, Pending, Cancelled.";
        }

        public static string PromptValid(string prompt, Func<string, string> validator,
                                          bool required = true, string currentValue = null)
        {
            while (true)
            {
                if (currentValue != null)
                    Console.Write("{0} [{1}]: ", prompt, currentValue);
                else
                    Console.Write("{0}: ", prompt);

                string input = Console.ReadLine();

                if (!required && string.IsNullOrWhiteSpace(input) && currentValue != null)
                    return currentValue;

                if (required && string.IsNullOrWhiteSpace(input) && currentValue == null)
                {
                    ConsoleHelper.WriteError("This field is required.");
                    continue;
                }

                if (!required && string.IsNullOrWhiteSpace(input))
                    return null;

                string error = validator(input);
                if (error != null)
                {
                    ConsoleHelper.WriteError(error);
                    continue;
                }

                return input.Trim();
            }
        }
    }


    //FileRepository

    public class FileRepository
    {
        private readonly string _dataFilePath;
        private readonly AuditLogger _logger;
        private int _nextIdCounter = 1;

        public FileRepository(string dataFilePath, AuditLogger logger)
        {
            _dataFilePath = dataFilePath;
            _logger = logger;
        }

        public void InitialiseCounter()
        {
            List<EventRegistration> all = ReadAll(true);
            foreach (EventRegistration r in all)
            {
                string numPart = r.RecordId.Replace("REG-", "");
                int n;
                if (int.TryParse(numPart, out n) && n >= _nextIdCounter)
                    _nextIdCounter = n + 1;
            }
        }

        public string GenerateId()
        {
            string id = string.Format("REG-{0:D5}", _nextIdCounter);
            _nextIdCounter++;
            return id;
        }

        public void Insert(EventRegistration record)
        {
            try { File.AppendAllText(_dataFilePath, record.ToCsvLine() + Environment.NewLine); }
            catch (IOException ex)
            {
                _logger.LogError("Insert failed for " + record.RecordId + ": " + ex.Message);
                throw;
            }
        }

        public List<EventRegistration> ReadAll(bool includeInactive = false)
        {
            List<EventRegistration> results = new List<EventRegistration>();
            if (!File.Exists(_dataFilePath)) return results;

            string[] lines;
            try { lines = File.ReadAllLines(_dataFilePath); }
            catch (IOException ex)
            {
                _logger.LogError("ReadAll IO error: " + ex.Message);
                return results;
            }

            int lineNum = 0;
            foreach (string line in lines)
            {
                lineNum++;
                if (string.IsNullOrWhiteSpace(line)) continue;
                try
                {
                    EventRegistration r = EventRegistration.FromCsvLine(line);
                    if (!ChecksumHelper.Verify(r))
                    {
                        _logger.LogError(string.Format(
                            "Checksum mismatch on line {0} (RecordId={1}).", lineNum, r.RecordId));
                        ConsoleHelper.WriteError(string.Format(
                            "[WARNING] Record on line {0} has invalid checksum and was skipped.", lineNum));
                        continue;
                    }
                    if (includeInactive || r.IsActive)
                        results.Add(r);
                }
                catch (Exception ex)
                {
                    _logger.LogError(string.Format(
                        "Malformed record on line {0}: {1}", lineNum, ex.Message));
                    ConsoleHelper.WriteError(string.Format(
                        "[WARNING] Could not parse record on line {0}. Skipping.", lineNum));
                }
            }
            return results;
        }

        public EventRegistration FindById(string recordId)
        {
            List<EventRegistration> all = ReadAll(false);
            foreach (EventRegistration r in all)
                if (string.Equals(r.RecordId, recordId, StringComparison.OrdinalIgnoreCase))
                    return r;
            return null;
        }

        public void Update(EventRegistration updated)
        {
            List<EventRegistration> all = ReadAll(true);
            bool found = false;
            for (int i = 0; i < all.Count; i++)
            {
                if (string.Equals(all[i].RecordId, updated.RecordId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    all[i] = updated;
                    found = true;
                    break;
                }
            }
            if (!found)
                throw new InvalidOperationException("Record not found for update: " + updated.RecordId);
            WriteAll(all);
        }

        public bool HardDelete(string recordId)
        {
            List<EventRegistration> all = ReadAll(true);
            int before = all.Count;
            all.RemoveAll(r => string.Equals(r.RecordId, recordId,
                                              StringComparison.OrdinalIgnoreCase));
            if (all.Count == before) return false;
            WriteAll(all);
            return true;
        }

        public List<EventRegistration> SearchByEventName(string keyword)
        {
            List<EventRegistration> results = new List<EventRegistration>();
            foreach (EventRegistration r in ReadAll())
                if (r.EventName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    results.Add(r);
            return results;
        }

        public List<EventRegistration> SearchByAttendeeName(string keyword)
        {
            List<EventRegistration> results = new List<EventRegistration>();
            foreach (EventRegistration r in ReadAll())
                if (r.AttendeeName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    results.Add(r);
            return results;
        }

        public List<EventRegistration> FilterByStatus(string status)
        {
            List<EventRegistration> results = new List<EventRegistration>();
            foreach (EventRegistration r in ReadAll())
                if (string.Equals(r.Status, status, StringComparison.OrdinalIgnoreCase))
                    results.Add(r);
            return results;
        }

        public List<EventRegistration> FilterByEventDate(string date)
        {
            List<EventRegistration> results = new List<EventRegistration>();
            foreach (EventRegistration r in ReadAll())
                if (r.EventDate == date)
                    results.Add(r);
            return results;
        }

        private void WriteAll(List<EventRegistration> records)
        {
            try
            {
                string temp = _dataFilePath + ".tmp";
                using (StreamWriter sw = new StreamWriter(temp, false))
                    foreach (EventRegistration r in records)
                        sw.WriteLine(r.ToCsvLine());
                File.Copy(temp, _dataFilePath, true);
                File.Delete(temp);
            }
            catch (IOException ex)
            {
                _logger.LogError("WriteAll IO error: " + ex.Message);
                throw;
            }
        }
    }


    //ReportGen

    public class ReportGenerator
    {
        private readonly FileRepository _repo;
        private readonly AuditLogger _logger;
        private readonly string _reportsFolder;

        public ReportGenerator(FileRepository repo, AuditLogger logger, string reportsFolder)
        {
            _repo = repo;
            _logger = logger;
            _reportsFolder = reportsFolder;
        }

        public void GenerateStatusSummaryReport()
        {
            List<EventRegistration> all = _repo.ReadAll(false);
            int confirmed = 0, pending = 0, cancelled = 0;
            foreach (EventRegistration r in all)
            {
                if (string.Equals(r.Status, "Confirmed", StringComparison.OrdinalIgnoreCase)) confirmed++;
                else if (string.Equals(r.Status, "Pending", StringComparison.OrdinalIgnoreCase)) pending++;
                else if (string.Equals(r.Status, "Cancelled", StringComparison.OrdinalIgnoreCase)) cancelled++;
            }

            List<string> lines = new List<string>();
            lines.Add(Header("REGISTRATION STATUS SUMMARY REPORT"));
            lines.Add(string.Format("  Generated  : {0}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
            lines.Add(string.Format("  Total Active Records : {0}", all.Count));
            lines.Add(Divider());
            lines.Add(string.Format("  {0,-15} {1,6}  ({2:P1})", "Confirmed:", confirmed,
                all.Count > 0 ? (double)confirmed / all.Count : 0));
            lines.Add(string.Format("  {0,-15} {1,6}  ({2:P1})", "Pending:", pending,
                all.Count > 0 ? (double)pending / all.Count : 0));
            lines.Add(string.Format("  {0,-15} {1,6}  ({2:P1})", "Cancelled:", cancelled,
                all.Count > 0 ? (double)cancelled / all.Count : 0));
            lines.Add(Divider());
            PrintAndSave("StatusSummary", lines);
            _logger.LogReport("StatusSummaryReport");
        }

        public void GenerateRegistrationsPerEventReport()
        {
            List<EventRegistration> all = _repo.ReadAll(false);
            Dictionary<string, int> counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (EventRegistration r in all)
            {
                if (!counts.ContainsKey(r.EventName)) counts[r.EventName] = 0;
                counts[r.EventName]++;
            }

            List<string> lines = new List<string>();
            lines.Add(Header("REGISTRATIONS PER EVENT REPORT"));
            lines.Add(string.Format("  Generated  : {0}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
            lines.Add(string.Format("  Distinct Events : {0}", counts.Count));
            lines.Add(Divider());
            lines.Add(string.Format("  {0,-40} {1,10}", "Event Name", "Registrations"));
            lines.Add(string.Format("  {0,-40} {1,10}", new string('-', 40), new string('-', 13)));
            foreach (KeyValuePair<string, int> kv in counts)
                lines.Add(string.Format("  {0,-40} {1,10}", Truncate(kv.Key, 40), kv.Value));
            lines.Add(Divider());
            PrintAndSave("RegistrationsPerEvent", lines);
            _logger.LogReport("RegistrationsPerEventReport");
        }

        public void GenerateUpcomingEventsReport()
        {
            List<EventRegistration> all = _repo.ReadAll(false);
            string today = DateTime.Today.ToString("yyyy-MM-dd");
            List<EventRegistration> upcoming = new List<EventRegistration>();
            foreach (EventRegistration r in all)
                if (string.Compare(r.EventDate, today, StringComparison.Ordinal) >= 0)
                    upcoming.Add(r);
            upcoming.Sort((a, b) => string.Compare(a.EventDate, b.EventDate, StringComparison.Ordinal));

            List<string> lines = new List<string>();
            lines.Add(Header("UPCOMING EVENTS REPORT"));
            lines.Add(string.Format("  Generated : {0}  |  Today: {1}",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), today));
            lines.Add(string.Format("  Upcoming Registrations : {0}", upcoming.Count));
            lines.Add(Divider());
            lines.Add(string.Format("  {0,-12} {1,-30} {2,-25} {3,-12}",
                "Date", "Event", "Attendee", "Status"));
            lines.Add(string.Format("  {0,-12} {1,-30} {2,-25} {3,-12}",
                new string('-', 12), new string('-', 30), new string('-', 25), new string('-', 12)));
            foreach (EventRegistration r in upcoming)
                lines.Add(string.Format("  {0,-12} {1,-30} {2,-25} {3,-12}",
                    r.EventDate, Truncate(r.EventName, 30), Truncate(r.AttendeeName, 25), r.Status));
            lines.Add(Divider());
            PrintAndSave("UpcomingEvents", lines);
            _logger.LogReport("UpcomingEventsReport");
        }

        public void GenerateFullListingReport()
        {
            List<EventRegistration> all = _repo.ReadAll(false);
            List<string> lines = new List<string>();
            lines.Add(Header("FULL ACTIVE REGISTRATIONS REPORT"));
            lines.Add(string.Format("  Generated  : {0}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
            lines.Add(string.Format("  Total Records : {0}", all.Count));
            lines.Add(Divider());
            foreach (EventRegistration r in all)
            {
                lines.Add(string.Format("  Record ID   : {0}", r.RecordId));
                lines.Add(string.Format("  Event       : {0}", r.EventName));
                lines.Add(string.Format("  Date        : {0}", r.EventDate));
                lines.Add(string.Format("  Attendee    : {0}", r.AttendeeName));
                lines.Add(string.Format("  Email       : {0}", r.Email));
                lines.Add(string.Format("  Contact     : {0}", r.ContactNumber));
                lines.Add(string.Format("  Status      : {0}", r.Status));
                lines.Add(string.Format("  Created At  : {0}", r.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")));
                lines.Add(string.Format("  Updated At  : {0}", r.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss")));
                lines.Add(string.Format("  Checksum    : {0}", r.Checksum));
                lines.Add(new string('-', 60));
            }
            PrintAndSave("FullListing", lines);
            _logger.LogReport("FullListingReport");
        }

       
        public void GeneratePersonalisedEventRankingReport()
        {
            List<EventRegistration> all = _repo.ReadAll(false);

            
            Dictionary<string, List<EventRegistration>> groups =
                new Dictionary<string, List<EventRegistration>>(StringComparer.OrdinalIgnoreCase);
            foreach (EventRegistration r in all)
            {
                if (!groups.ContainsKey(r.EventName))
                    groups[r.EventName] = new List<EventRegistration>();
                groups[r.EventName].Add(r);
            }

           
            List<KeyValuePair<string, List<EventRegistration>>> sorted =
                new List<KeyValuePair<string, List<EventRegistration>>>(groups);
            sorted.Sort((a, b) => b.Value.Count.CompareTo(a.Value.Count));

            List<string> lines = new List<string>();
            lines.Add(Header("PERSONALISED REPORT: EVENT RANKING BY REGISTRATIONS"));
            lines.Add("  Rule: Events ranked by total registrations (descending).");
            lines.Add("  Events with confirmed rate below 50% are flagged [LOW CONFIRM].");
            lines.Add(string.Format("  Generated : {0}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
            lines.Add(string.Format("  Total Events : {0}  |  Total Registrations : {1}", groups.Count, all.Count));
            lines.Add(Divider());
            lines.Add(string.Format("  {0,-3} {1,-35} {2,5} {3,5} {4,5} {5,5}  {6}",
                "#", "Event Name", "Total", "Conf", "Pend", "Canc", "Flag"));
            lines.Add(string.Format("  {0,-3} {1,-35} {2,5} {3,5} {4,5} {5,5}  {6}",
                "---", new string('-', 35), "-----", "-----", "-----", "-----", "----------"));

            int rank = 1;
            foreach (KeyValuePair<string, List<EventRegistration>> kv in sorted)
            {
                int total = kv.Value.Count;
                int conf = 0, pend = 0, canc = 0;
                foreach (EventRegistration r in kv.Value)
                {
                    if (string.Equals(r.Status, "Confirmed", StringComparison.OrdinalIgnoreCase)) conf++;
                    else if (string.Equals(r.Status, "Pending", StringComparison.OrdinalIgnoreCase)) pend++;
                    else if (string.Equals(r.Status, "Cancelled", StringComparison.OrdinalIgnoreCase)) canc++;
                }
                double confirmRate = total > 0 ? (double)conf / total : 0;
                string flag = confirmRate < 0.5 ? "[LOW CONFIRM]" : string.Empty;
                lines.Add(string.Format("  {0,-3} {1,-35} {2,5} {3,5} {4,5} {5,5}  {6}",
                    rank, Truncate(kv.Key, 35), total, conf, pend, canc, flag));
                rank++;
            }

            lines.Add(Divider());
            lines.Add("  Legend: Conf=Confirmed  Pend=Pending  Canc=Cancelled");
            lines.Add("  [LOW CONFIRM] = less than 50% of registrations are Confirmed.");
            lines.Add(Divider());
            PrintAndSave("PersonalisedEventRanking", lines);
            _logger.LogReport("PersonalisedEventRankingReport");
        }

        private void PrintAndSave(string reportName, List<string> lines)
        {
            Console.WriteLine();
            foreach (string line in lines) Console.WriteLine(line);
            string fileName = string.Format("{0}_{1}.txt", reportName,
                DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            string filePath = Path.Combine(_reportsFolder, fileName);
            try
            {
                File.WriteAllLines(filePath, lines.ToArray());
                ConsoleHelper.WriteSuccess("\nReport saved to: " + filePath);
            }
            catch (IOException ex)
            {
                _logger.LogError("Report save failed: " + ex.Message);
                ConsoleHelper.WriteError("Could not save report file: " + ex.Message);
            }
        }

        private static string Header(string title)
        {
            string border = new string('=', 70);
            return string.Format("{0}\n  {1}\n{0}", border, title.ToUpper());
        }

        private static string Divider() { return new string('-', 70); }

        private static string Truncate(string value, int max)
        {
            if (value == null) return string.Empty;
            if (value.Length <= max) return value;
            return value.Substring(0, max - 3) + "...";
        }
    }


    //Menu

    public class MenuController
    {
        private static readonly string DataFolder = "Data";
        private static readonly string ReportsFolder = "Reports";
        private static readonly string DataFilePath = Path.Combine(DataFolder, "registrations.txt");
        private static readonly string AuditFilePath = Path.Combine(DataFolder, "audit.log");

        private AuditLogger _logger;
        private FileRepository _repo;
        private ReportGenerator _reporter;

        public void Run()
        {
            StorageInitializer.Initialise(DataFolder, DataFilePath, AuditFilePath, ReportsFolder);
            _logger = new AuditLogger(AuditFilePath);
            _repo = new FileRepository(DataFilePath, _logger);
            _reporter = new ReportGenerator(_repo, _logger, ReportsFolder);
            _repo.InitialiseCounter();
            _logger.LogSystem("Application started.");
            ShowBanner();

            bool running = true;
            while (running)
            {
                ShowMainMenu();
                string choice = (Console.ReadLine() ?? string.Empty).Trim();
                switch (choice)
                {
                    case "1": AddRecord(); break;
                    case "2": ViewRecords(); break;
                    case "3": SearchRecords(); break;
                    case "4": UpdateRecord(); break;
                    case "5": SoftDeleteRecord(); break;
                    case "6": ShowReportMenu(); break;
                    case "7": ViewAuditLog(); break;
                    case "8": HardDeleteRecord(); break;
                    case "0":
                        running = false;
                        _logger.LogSystem("Application exited by user.");
                        ConsoleHelper.WriteSuccess("\nGoodbye! All data has been saved.\n");
                        break;
                    default:
                        ConsoleHelper.WriteWarning("Invalid option. Please enter a number from the menu.");
                        break;
                }
            }
        }

        private void ShowBanner()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  ============================================================");
            Console.WriteLine("        EVENT REGISTRATION MANAGEMENT SYSTEM");
            Console.WriteLine("  ============================================================");
            Console.ResetColor();
        }

        private void ShowMainMenu()
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("  ──────────── MAIN MENU ─────────────");
            Console.ResetColor();
            Console.WriteLine("  [1] Add New Registration");
            Console.WriteLine("  [2] View All Registrations");
            Console.WriteLine("  [3] Search / Filter Registrations");
            Console.WriteLine("  [4] Update Registration");
            Console.WriteLine("  [5] Delete Registration  (soft delete)");
            Console.WriteLine("  [6] Generate Report");
            Console.WriteLine("  [7] View Audit Log (last 20 entries)");
            Console.WriteLine("  [8] Hard Delete Registration  (permanent)");
            Console.WriteLine("  [0] Exit");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("\n  Enter choice: ");
            Console.ResetColor();
        }

        private void AddRecord()
        {
            ConsoleHelper.WriteHeader("Add New Registration");
            try
            {
                EventRegistration r = new EventRegistration();
                r.EventName = Validator.PromptValid("  Event Name", Validator.ValidateEventName);
                r.AttendeeName = Validator.PromptValid("  Attendee Name", Validator.ValidateAttendeeName);
                r.Email = Validator.PromptValid("  Email Address", Validator.ValidateEmail);
                r.ContactNumber = Validator.PromptValid("  Contact Number", Validator.ValidateContactNumber);
                r.EventDate = Validator.PromptValid("  Event Date (yyyy-MM-dd)", Validator.ValidateEventDate);
                Console.WriteLine("  Status options: Confirmed | Pending | Cancelled");
                r.Status = Validator.PromptValid("  Status", Validator.ValidateStatus);
                r.RecordId = _repo.GenerateId();
                r.CreatedAt = DateTime.Now;
                r.UpdatedAt = DateTime.Now;
                r.IsActive = true;
                r.Checksum = ChecksumHelper.Compute(r);
                _repo.Insert(r);
                _logger.LogAdd(r.RecordId, string.Format("Event={0}, Attendee={1}, Date={2}, Status={3}",
                    r.EventName, r.AttendeeName, r.EventDate, r.Status));
                ConsoleHelper.WriteSuccess("\n  Registration added successfully!  ID: " + r.RecordId);
            }
            catch (Exception ex)
            {
                _logger.LogError("AddRecord: " + ex.Message);
                ConsoleHelper.WriteError("  Error adding record: " + ex.Message);
            }
            ConsoleHelper.PressAnyKey();
        }

        private void ViewRecords()
        {
            ConsoleHelper.WriteHeader("All Active Registrations");
            try
            {
                List<EventRegistration> records = _repo.ReadAll();
                _logger.LogRead(string.Format("ViewAll - {0} records returned.", records.Count));
                if (records.Count == 0)
                    ConsoleHelper.WriteWarning("  No active registrations found.");
                else
                {
                    Console.WriteLine(string.Format("  {0} record(s) found.\n", records.Count));
                    foreach (EventRegistration r in records)
                        ConsoleHelper.PrintRecord(r);
                    ConsoleHelper.WriteDivider();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("ViewRecords: " + ex.Message);
                ConsoleHelper.WriteError("  Error reading records: " + ex.Message);
            }
            ConsoleHelper.PressAnyKey();
        }

        private void SearchRecords()
        {
            ConsoleHelper.WriteHeader("Search / Filter Registrations");
            Console.WriteLine("  [1] Event Name (keyword)");
            Console.WriteLine("  [2] Attendee Name (keyword)");
            Console.WriteLine("  [3] Status  (Confirmed / Pending / Cancelled)");
            Console.WriteLine("  [4] Event Date  (yyyy-MM-dd)");
            Console.Write("\n  Enter choice: ");
            string choice = (Console.ReadLine() ?? string.Empty).Trim();
            List<EventRegistration> results = new List<EventRegistration>();
            string filterDesc = string.Empty;

            try
            {
                switch (choice)
                {
                    case "1":
                        Console.Write("  Keyword: ");
                        string eventKw = (Console.ReadLine() ?? string.Empty).Trim();
                        results = _repo.SearchByEventName(eventKw);
                        filterDesc = "EventName LIKE '" + eventKw + "'";
                        break;
                    case "2":
                        Console.Write("  Keyword: ");
                        string nameKw = (Console.ReadLine() ?? string.Empty).Trim();
                        results = _repo.SearchByAttendeeName(nameKw);
                        filterDesc = "AttendeeName LIKE '" + nameKw + "'";
                        break;
                    case "3":
                        Console.Write("  Status: ");
                        string status = (Console.ReadLine() ?? string.Empty).Trim();
                        results = _repo.FilterByStatus(status);
                        filterDesc = "Status='" + status + "'";
                        break;
                    case "4":
                        Console.Write("  Event Date (yyyy-MM-dd): ");
                        string date = (Console.ReadLine() ?? string.Empty).Trim();
                        results = _repo.FilterByEventDate(date);
                        filterDesc = "EventDate='" + date + "'";
                        break;
                    default:
                        ConsoleHelper.WriteWarning("  Invalid search option.");
                        ConsoleHelper.PressAnyKey();
                        return;
                }

                _logger.LogRead(string.Format("Search [{0}] - {1} result(s).", filterDesc, results.Count));
                Console.WriteLine(string.Format("\n  {0} result(s) for: {1}", results.Count, filterDesc));

                if (results.Count == 0)
                    ConsoleHelper.WriteWarning("  No records matched.");
                else
                {
                    foreach (EventRegistration r in results)
                        ConsoleHelper.PrintRecord(r);
                    ConsoleHelper.WriteDivider();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("SearchRecords: " + ex.Message);
                ConsoleHelper.WriteError("  Error during search: " + ex.Message);
            }
            ConsoleHelper.PressAnyKey();
        }

        private void UpdateRecord()
        {
            ConsoleHelper.WriteHeader("Update Registration");
            Console.Write("  Enter Record ID (e.g. REG-00001): ");
            string id = (Console.ReadLine() ?? string.Empty).Trim().ToUpper();
            try
            {
                EventRegistration r = _repo.FindById(id);
                if (r == null)
                {
                    ConsoleHelper.WriteError("  Record not found: " + id);
                    _logger.LogError("Update - record not found: " + id);
                    ConsoleHelper.PressAnyKey();
                    return;
                }
                Console.WriteLine("\n  Current record:");
                ConsoleHelper.PrintRecord(r);
                Console.WriteLine("\n  Press ENTER to keep current value.\n");

                r.EventName = Validator.PromptValid("  Event Name", Validator.ValidateEventName, false, r.EventName) ?? r.EventName;
                r.AttendeeName = Validator.PromptValid("  Attendee Name", Validator.ValidateAttendeeName, false, r.AttendeeName) ?? r.AttendeeName;
                r.Email = Validator.PromptValid("  Email Address", Validator.ValidateEmail, false, r.Email) ?? r.Email;
                r.ContactNumber = Validator.PromptValid("  Contact Number", Validator.ValidateContactNumber, false, r.ContactNumber) ?? r.ContactNumber;
                r.EventDate = Validator.PromptValid("  Event Date (yyyy-MM-dd)", Validator.ValidateEventDate, false, r.EventDate) ?? r.EventDate;
                Console.WriteLine("  Status options: Confirmed | Pending | Cancelled");
                r.Status = Validator.PromptValid("  Status", Validator.ValidateStatus, false, r.Status) ?? r.Status;

                r.UpdatedAt = DateTime.Now;
                r.Checksum = ChecksumHelper.Compute(r);
                _repo.Update(r);
                _logger.LogUpdate(r.RecordId, string.Format("Event={0}, Attendee={1}, Date={2}, Status={3}",
                    r.EventName, r.AttendeeName, r.EventDate, r.Status));
                ConsoleHelper.WriteSuccess("\n  Record updated successfully!");
            }
            catch (Exception ex)
            {
                _logger.LogError("UpdateRecord " + id + ": " + ex.Message);
                ConsoleHelper.WriteError("  Error updating record: " + ex.Message);
            }
            ConsoleHelper.PressAnyKey();
        }

        private void SoftDeleteRecord()
        {
            ConsoleHelper.WriteHeader("Soft Delete Registration");
            Console.Write("  Enter Record ID to deactivate: ");
            string id = (Console.ReadLine() ?? string.Empty).Trim().ToUpper();
            try
            {
                EventRegistration r = _repo.FindById(id);
                if (r == null)
                {
                    ConsoleHelper.WriteError("  Record not found or already inactive: " + id);
                    _logger.LogError("SoftDelete - record not found: " + id);
                    ConsoleHelper.PressAnyKey();
                    return;
                }
                Console.WriteLine("\n  Record to deactivate:");
                ConsoleHelper.PrintRecord(r);
                Console.Write("\n  Confirm soft delete? (Y/N): ");
                string confirm = (Console.ReadLine() ?? "N").Trim().ToUpper();
                if (confirm != "Y")
                {
                    ConsoleHelper.WriteWarning("  Soft delete cancelled.");
                    ConsoleHelper.PressAnyKey();
                    return;
                }
                r.IsActive = false;
                r.UpdatedAt = DateTime.Now;
                r.Checksum = ChecksumHelper.Compute(r);
                _repo.Update(r);
                _logger.LogSoftDelete(id);
                ConsoleHelper.WriteSuccess("\n  Record marked as inactive.");
            }
            catch (Exception ex)
            {
                _logger.LogError("SoftDelete " + id + ": " + ex.Message);
                ConsoleHelper.WriteError("  Error during soft delete: " + ex.Message);
            }
            ConsoleHelper.PressAnyKey();
        }

        private void ShowReportMenu()
        {
            ConsoleHelper.WriteHeader("Report Generator");
            Console.WriteLine("  [1] Registration Status Summary");
            Console.WriteLine("  [2] Registrations Per Event");
            Console.WriteLine("  [3] Upcoming Events");
            Console.WriteLine("  [4] Full Active Records Listing");
            Console.WriteLine("  [5] Personalised: Event Ranking by Registrations  (flags low-confirm events)");
            Console.Write("\n  Enter choice: ");
            string choice = (Console.ReadLine() ?? string.Empty).Trim();
            try
            {
                switch (choice)
                {
                    case "1": _reporter.GenerateStatusSummaryReport(); break;
                    case "2": _reporter.GenerateRegistrationsPerEventReport(); break;
                    case "3": _reporter.GenerateUpcomingEventsReport(); break;
                    case "4": _reporter.GenerateFullListingReport(); break;
                    case "5": _reporter.GeneratePersonalisedEventRankingReport(); break;
                    default: ConsoleHelper.WriteWarning("  Invalid report option."); break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Report: " + ex.Message);
                ConsoleHelper.WriteError("  Error generating report: " + ex.Message);
            }
            ConsoleHelper.PressAnyKey();
        }

        private void ViewAuditLog()
        {
            ConsoleHelper.WriteHeader("Audit Log (Last 20 Entries)");
            try
            {
                if (!File.Exists(AuditFilePath))
                {
                    ConsoleHelper.WriteWarning("  Audit log file not found.");
                    ConsoleHelper.PressAnyKey();
                    return;
                }
                string[] lines = File.ReadAllLines(AuditFilePath);
                int start = Math.Max(0, lines.Length - 20);
                for (int i = start; i < lines.Length; i++)
                    Console.WriteLine("  " + lines[i]);
            }
            catch (Exception ex)
            {
                _logger.LogError("ViewAuditLog: " + ex.Message);
                ConsoleHelper.WriteError("  Could not read audit log: " + ex.Message);
            }
            ConsoleHelper.PressAnyKey();
        }

        private void HardDeleteRecord()
        {
            ConsoleHelper.WriteHeader("Hard Delete Registration  [PERMANENT]");
            ConsoleHelper.WriteWarning("  WARNING: This action cannot be undone!");
            Console.Write("\n  Enter Record ID to permanently delete: ");
            string id = (Console.ReadLine() ?? string.Empty).Trim().ToUpper();
            try
            {
                List<EventRegistration> all = _repo.ReadAll(true);
                EventRegistration found = null;
                foreach (EventRegistration r in all)
                    if (string.Equals(r.RecordId, id, StringComparison.OrdinalIgnoreCase))
                    { found = r; break; }

                if (found == null)
                {
                    ConsoleHelper.WriteError("  Record not found: " + id);
                    _logger.LogError("HardDelete - not found: " + id);
                    ConsoleHelper.PressAnyKey();
                    return;
                }
                Console.WriteLine("\n  Record to permanently delete:");
                ConsoleHelper.PrintRecord(found);
                Console.Write("\n  Type DELETE to confirm: ");
                string confirm = (Console.ReadLine() ?? string.Empty).Trim();
                if (confirm != "DELETE")
                {
                    ConsoleHelper.WriteWarning("  Hard delete cancelled.");
                    ConsoleHelper.PressAnyKey();
                    return;
                }
                bool ok = _repo.HardDelete(id);
                if (ok)
                {
                    _logger.LogHardDelete(id);
                    ConsoleHelper.WriteSuccess("\n  Record permanently deleted: " + id);
                }
                else
                    ConsoleHelper.WriteError("  Could not delete record.");
            }
            catch (Exception ex)
            {
                _logger.LogError("HardDelete " + id + ": " + ex.Message);
                ConsoleHelper.WriteError("  Error during hard delete: " + ex.Message);
            }
            ConsoleHelper.PressAnyKey();
        }
    }



    class Program
    {
        static void Main(string[] args)
        {
            MenuController controller = new MenuController();
            controller.Run();
        }
    }
}