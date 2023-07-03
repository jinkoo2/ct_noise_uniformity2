using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ct_noise_uniformity_lib
{
    public class fswatcher
    {

        public fswatcher(string service_param_file)
        {
            try
            {
                global_variables.service_param = new param(service_param_file);
                global_variables.log_path = global_variables.service_param.get_value("log_path");
                global_variables.log_line("log_path=" + global_variables.log_path);
            }
            catch (Exception exn)
            {
                log_line(exn.ToString());
            }
        }
        
        public void start_watching()
        {
            try
            {
                // watch path
                string watch_path = global_variables.service_param.get_value("watch_path");
                global_variables.log_path = global_variables.service_param.get_value("log_path");
                global_variables.log_line("watch_path=" + watch_path);

                // Create a new FileSystemWatcher and set its properties.
                FileSystemWatcher watcher = new FileSystemWatcher();
                watcher.Path = watch_path;
                /* Watch for directory creaation*/
                watcher.NotifyFilter =
                    NotifyFilters.CreationTime |
                    NotifyFilters.DirectoryName;

                watcher.IncludeSubdirectories = false;

                // Add event handlers.
                //watcher.Changed += new FileSystemEventHandler(OnChanged);
                log_line("subscribing to create event:" + watch_path + ".");

                watcher.Created += new FileSystemEventHandler(OnChanged);
                //watcher.Deleted += new FileSystemEventHandler(OnChanged);
                watcher.Renamed += new RenamedEventHandler(OnRenamed);

                // Begin watching.
                watcher.EnableRaisingEvents = true;
            }
            catch (Exception exn)
            {
                log_line(exn.ToString());
            }
        }

        private void log_line(string msg)
        {
            global_variables.log_line(msg);
        }

        private void log(string msg)
        {
            global_variables.log(msg);
        }

       

        public void Run(string import_dir)
        {
            // import_dir
            log_line("import_dir=" + import_dir);
            if(!System.IO.Directory.Exists(import_dir))
            {
                global_variables.log_error("Error: import directory not found. import_dir=" + import_dir);
                return;
            }

            /////////////////////////////////////////////////
            //// sort the data by patient id, study, series
            string dicom_sort_base_dir = global_variables.service_param.get_value("dicom_sort_base_dir");

            if(!System.IO.Directory.Exists(dicom_sort_base_dir))
            {
                log_line("directory not found, so creating directory: " + dicom_sort_base_dir);
                System.IO.Directory.CreateDirectory(dicom_sort_base_dir);
            }

            string sort_dir = System.IO.Path.Combine(dicom_sort_base_dir, global_variables.make_date_time_string_now());

            global_variables.log_line("creating sort_dir=" + sort_dir);
            System.IO.Directory.CreateDirectory(sort_dir);

            global_variables.log_line("sorting dicom files...patient->study->series...");
            dicomtools.sort_files_by_patient_study_series(import_dir, sort_dir, "no");

            ///////////////////////////////////////
            // find the CT series of this patient
            global_variables.log_line("seraching CT data to process...");
            string case_dir = "";
            foreach (string pt_dir in System.IO.Directory.GetDirectories(sort_dir))
            {
                global_variables.log_line("pt_dir="+pt_dir);
                foreach (string study_dir in System.IO.Directory.GetDirectories(pt_dir))
                {
                    global_variables.log_line("study_dir=" + study_dir);
                    foreach (string series_dir in System.IO.Directory.GetDirectories(study_dir))
                    {
                        global_variables.log_line("series_dir=" + series_dir);

                        // load info file
                        param p = new param(System.IO.Path.Combine(series_dir, "info.txt"));
                        string PatientID = p.get_value("PatientID");
                        string PatientName = p.get_value("PatientName");
                        string StationName = p.get_value("StationName");
                        string SeriesDescription = p.get_value("SeriesDescription");
                        string StudyDate = p.get_value("StudyDate");
                        string StudyTime = p.get_value("StudyTime");
                        global_variables.log_line("PatientID=" + PatientID);
                        global_variables.log_line("PatientName=" + PatientName);
                        global_variables.log_line("StationName=" + StationName);
                        global_variables.log_line("SeriesDescription=" + SeriesDescription);
                        global_variables.log_line("StudyDate=" + StudyDate);
                        global_variables.log_line("StudyTime=" + StudyTime);


                        //mahcine directory
                        string machine_key = string.Format("{0}", StationName.Trim().ToLower());
                        string machine_dir = global_variables.service_param.get_value(machine_key);
                        if (machine_dir == "")
                        {
                            global_variables.log_line("machine_dir not found in the service param file. Make sure there is a key-value pair with key=" + machine_key + ". Skipping...");
                            continue;
                        }
                        if (!System.IO.Directory.Exists(machine_dir))
                        {
                            global_variables.log_line("machine_dir does not exist: " + machine_dir + ". Skipping...");
                            continue;
                        }
                        string machine_param_file = System.IO.Path.Combine(machine_dir, "machine_param.txt");
                        if (!System.IO.File.Exists(machine_param_file))
                        {
                            global_variables.log_line("param file does not exist: " + machine_param_file + ". Skipping...");
                            continue;
                        }

                        global_variables.machine_param = new param(machine_param_file);

                        global_variables.log_line("machine param file=" + machine_param_file);


                        // create a case dir
                        string cases_dir = global_variables.combine(machine_dir, "cases");
                        case_dir = global_variables.combine(cases_dir, string.Format("{0}_{1}_{2}", StudyDate, StudyTime,PatientName.Split('^')[1]));
                        global_variables.log_line("case_dir=" + case_dir);
                        System.IO.Directory.CreateDirectory(case_dir);

                        // copy files
                        string desc = p.get_value("SeriesDescription").ToLower();
                        {
                            global_variables.log_line("series description=" + desc);

                            // copy dire                            
                            string dst_dir = global_variables.combine(case_dir, desc);
                            System.IO.Directory.CreateDirectory(dst_dir);
                            global_variables.copy_files(series_dir, dst_dir, true);
                        }
                    } // for series
                }// for study
            } // for patient

            // run
            ct_noise_uniformity_lib.noise_uniformity qa = new noise_uniformity();
            qa.run(case_dir);
        }


        private void Update(string changed, bool renamed)
        {
            try
            {
                string dir = changed;

                if(!dir.ToLower().Contains("_mpc"))
                {
                    log_line("Not a MPC folder, skipping...");
                    return;
                }

                // wait until files are ready to process
                int notification_to_process_sec = System.Convert.ToInt32(global_variables.service_param.get_value("notification_to_process_sec"));
                log_line("waiting "+ notification_to_process_sec + " seconds to be sure that all images are saved...");
                Thread.Sleep(notification_to_process_sec * 1000); // wait some seconds 
                
                Run(changed);
            }
            catch (Exception exn)
            {
                log_line(exn.ToString());
            }
        }

        // Define the event handlers.
        private void OnChanged(object source, FileSystemEventArgs e)
        {
            // Specify what is done when a file is changed, created, or deleted.
            log_line("Folder: " + e.FullPath + " " + e.ChangeType);

            Update(e.FullPath, false);
        }

        private void OnRenamed(object source, RenamedEventArgs e)
        {
            // Specify what is done when a file is renamed.
            log_line(string.Format("File: {0} renamed to {1}", e.OldFullPath, e.FullPath));

            Update(e.FullPath, true);
        }
    }
}
