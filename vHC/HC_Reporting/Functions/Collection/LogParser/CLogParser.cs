﻿// Copyright (c) 2021, Adam Congdon <adam.congdon2@gmail.com>
// MIT License
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using VeeamHealthCheck.Functions.Collection.DB;
using VeeamHealthCheck.Shared;
using VeeamHealthCheck.Shared.Logging;

namespace VeeamHealthCheck.Functions.Collection.LogParser
{
    class CLogParser
    {
        private CLogger log = CGlobals.Logger;
        private string LogLocation;
        private Dictionary<string, List<TimeSpan>> _waits = new();
        private string _pathToCsv = CVariables.vbrDir + @"\waits.csv";

        private List<string> _fixList = new();

        private string logStart = "[LogParser] ";

        public CLogParser()
        {
            LogLocation = InitLogDir();
            InitWaitCsv();
            /*check each job for long wait times
             * need:
             * - job name
             * - how long it waited
             * -- last wait
             * -- times waited
             * -- avg wait?
             * - overlaps?
             * 
             */

        }
        public CLogParser(string path)
        {

        }

        public string InitLogDir()
        {
            log.Info(logStart + "Checking registry for default log location...");
            string logs = "";
            try
            {
                CRegReader reg = new();
                logs = reg.DefaultLogDir();
                log.Info(logStart + "Log Location: " + logs);
            }
            catch (Exception e)
            {
                log.Error(logStart + "Failed to return log location.");
            }
            return logs;
        }
        private void InitWaitCsv()
        {
            log.Info(logStart + "Init waits.csv");
            try
            {
                using (StreamWriter sw = new StreamWriter(_pathToCsv, append: false))
                {
                    sw.WriteLine("JobName,StartTime,EndTime,Duration");
                }
            }
            catch (Exception)
            {
                log.Error(logStart + "Failed to init waits.csv");
            }
        }
        private void DumpWaitsToFile(string JobName, DateTime start, DateTime end, TimeSpan diff)
        {

            using (StreamWriter sw = new StreamWriter(_pathToCsv, append: true))
            {
                sw.WriteLine(JobName + "," + start + "," + end + "," + diff);
            }

            //String csv = String.Join(
            //    Environment.NewLine,
            //    _waits.Select(d => $"{d.Key};{d.Value};")
            //        );
            //System.IO.File.WriteAllText(pathToCsv, csv);
        }
        public Dictionary<string, List<TimeSpan>> GetWaitsFromFiles()
        {
            log.Info("Checking Log files for waits..", false);
            Dictionary<string, List<TimeSpan>> jobsAndWaits = new();
            string[] dirList = Directory.GetDirectories(LogLocation);

            int counter = 0;
            foreach (var d in dirList)
            {
                counter++;
                string info = string.Format("[LogParser] Parsing log {0} of {1}", counter, dirList.Count());
                log.Info(info, false);
                string jobname = Path.GetFileName(d);

                List<TimeSpan> waits = new();

                string[] fileList = Directory.GetFiles(d, "*.log", SearchOption.AllDirectories);

                foreach (var f in fileList)
                {
                    waits.AddRange(CheckFileWait(f, jobname));
                }
                jobsAndWaits.Add(jobname, waits);
            }
            _waits = jobsAndWaits;
            log.Info("Checking Log files for waits..Done!", false);
            return jobsAndWaits;
        }
        private void ParseFixLines(string line)
        {
            try
            {

                //log.Debug(line, false);
                string fixLine = line.Remove(0, line.IndexOf("Private Fix"));
                if (fixLine.EndsWith(']'))
                    fixLine = fixLine.Replace("]", "");
                if (!_fixList.Contains(fixLine))
                {
                    _fixList.Add(fixLine);
                    log.Debug(fixLine, false);

                }
            }
            catch (Exception e) { log.Debug(e.Message); }
        }
        private List<TimeSpan> CheckFileWait(string file, string jobName)
        {
            List<TimeSpan> diffListMin = new();

            using (StreamReader sr = new(file))
            {
                string line;
                string waitLine = "Waiting for backup infrastructure resources availability";
                string startTime = null;
                string endTime = null;

                bool countNextLine = false;
                while ((line = sr.ReadLine()) != null)
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        if (line.Contains("Private Fix"))
                            ParseFixLines(line);
                        try
                        {
                            if (countNextLine)
                            {
                                endTime = line.Remove(21);

                                countNextLine = false;

                            }

                            string trimline = line.Substring(40);
                            if (trimline == waitLine)
                            {
                                startTime = line.Remove(21);
                                countNextLine = true;
                            }
                            if (!string.IsNullOrEmpty(startTime) && !string.IsNullOrEmpty(endTime))
                            {
                                diffListMin.Add(CalcTime(jobName, startTime, endTime));
                                endTime = "";
                                startTime = "";
                            }
                        }
                        catch (Exception e) { }
                    }

                }
            }
            return diffListMin;

        }

        private TimeSpan CalcTime(string jobName, string start, string end)
        {
            start = start.Trim('[');
            start = start.Trim(']');
            end = end.Trim('[');
            end = end.Trim(']');


            //DateTime.TryParse(start, out DateTime tStart);
            //DateTime.TryParse(end, out DateTime tEnd);

            DateTime.TryParseExact(start, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime tStart);
            DateTime.TryParseExact(end, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime tEnd);

            var diffTime = tEnd - tStart;
            //string t = diffTime.ToString("dd:HH:mm:ss");
            DumpWaitsToFile(jobName, tStart, tEnd, diffTime);
            return diffTime;
        }

    }
}