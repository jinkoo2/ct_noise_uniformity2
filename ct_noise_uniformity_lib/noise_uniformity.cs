using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ct_noise_uniformity_lib
{
    public class noise_uniformity
    {
        string combine(string path1, string path2)
        {
            return System.IO.Path.Combine(path1, path2);
        }

        void delete_file(string path)
        {
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }

        public void process_baseline()
        {
            string baseline_dir = global_variables.machine_param.get_value("baseline_dir");
            analize(baseline_dir, combine(baseline_dir, "out"));
        }

        double[] get_point(string file, string pt_name)
        {
            param p_pts = new param(file);
            return toDouble(p_pts.get_value_as_array(pt_name));
        }

        string pass_fail(double[] err, double tol)
        {
            foreach(double value in err)
            {
                if (Math.Abs(value) > tol)
                    return "fail";
            }

            return "pass";
        }

        string print_line(string title, double[] pt, double[] pt_ref, double tol, string num_format)
        {
            double[] err = sub(pt, pt_ref);

            return string.Format("{0},{1},{2},{3},{4},{5}",
                    title,
                    toString(pt, num_format),
                    toString(pt_ref, num_format),
                    toString(sub(pt,pt_ref), num_format),
                    norm(err).ToString(num_format),
                    pass_fail(err, tol));
        }

        public void run(string case_dir)
        {
            global_variables.log_line("run()");
            global_variables.log_line("case_dir=" + case_dir);

            //////////////////////////
            // do the analysis
            global_variables.log_line("analizing data...");
            string out_dir = combine(case_dir, "out");

            global_variables.log_line("creating folder out_dir="+out_dir);
            System.IO.Directory.CreateDirectory(out_dir);
            analize(case_dir, out_dir);

            string num_format = "0.0";

            string[] ct_series_descriptions = global_variables.machine_param.get_value_as_array("ct_series_descriptions");
            string[] mask_names = global_variables.machine_param.get_value_as_array("mask_names");
            double uniformity_HU_tol = System.Convert.ToDouble(global_variables.machine_param.get_value("uniformity_HU_tol"));

            /////////////////
            // mean table
            {
                StringBuilder sb = new StringBuilder();

                // header
                sb.AppendLine("Loc," + string.Join(",", ct_series_descriptions));
                foreach (string mask_name in mask_names)
                {
                    List<string> list = new List<string>();
                    foreach (string dirname in ct_series_descriptions)
                    {
                        string img_dir = combine(case_dir, dirname.ToLower());
                        string mean_std_file = combine(img_dir, "mean_std.txt");
                        param p = new param(mean_std_file);
                        string[] mean_std = p.get_value_as_array(mask_name + "_mean_std");
                        string mean = toString(mean_std[0], num_format);
                        list.Add(mean);
                    }
                    sb.AppendLine(mask_name + "," + string.Join(",", list.ToArray()));
                }

                //max diff
                {
                    List<string> list = new List<string>();
                    foreach (string dirname in ct_series_descriptions)
                    {
                        string img_dir = combine(case_dir, dirname.ToLower());
                        string mean_std_file = combine(img_dir, "mean_std.txt");
                        param p = new param(mean_std_file);
                        string max_diff_from_center = toString(p.get_value("max diff from center"), num_format);
                        list.Add(max_diff_from_center);
                    }
                    sb.AppendLine("Max. Diff. from Center," + string.Join(",", list.ToArray()));
                }

                //max diff, pass or fail
                {
                    List<string> list = new List<string>();
                    foreach (string dirname in ct_series_descriptions)
                    {
                        string img_dir = combine(case_dir, dirname.ToLower());
                        string mean_std_file = combine(img_dir, "mean_std.txt");
                        param p = new param(mean_std_file);
                        double max_diff_from_center = System.Convert.ToDouble(p.get_value("max diff from center"));
                        if (max_diff_from_center > uniformity_HU_tol)
                            list.Add("FAIL");
                        else
                            list.Add("PASS");
                    }
                    sb.AppendLine("Uniformity," + string.Join(",", list.ToArray()));
                }

                // write
                System.IO.File.WriteAllText(combine(out_dir, "mean.csv"), sb.ToString());
            }


            /////////////////
            // STD table
            {
                StringBuilder sb = new StringBuilder();

                // header
                sb.AppendLine("Loc," + string.Join(",", ct_series_descriptions));
                foreach (string mask_name in mask_names)
                {
                    List<string> list = new List<string>();
                    foreach (string dirname in ct_series_descriptions)
                    {
                        string img_dir = combine(case_dir, dirname.ToLower());
                        string mean_std_file = combine(img_dir, "mean_std.txt");
                        param p = new param(mean_std_file);
                        string[] mean_std = p.get_value_as_array(mask_name + "_mean_std");
                        string std = toString(mean_std[1], num_format);
                        list.Add(std);
                    }
                    sb.AppendLine(mask_name + "," + string.Join(",", list.ToArray()));
                }

                //avg std
                {
                    List<string> list = new List<string>();
                    foreach (string dirname in ct_series_descriptions)
                    {
                        string img_dir = combine(case_dir, dirname.ToLower());
                        string mean_std_file = combine(img_dir, "mean_std.txt");
                        param p = new param(mean_std_file);
                        string avg = toString(p.get_value("avg of STDs"), num_format);
                        list.Add(avg);
                    }
                    sb.AppendLine("Mean of STDs," + string.Join(",", list.ToArray()));
                }

                // write
                System.IO.File.WriteAllText(combine(out_dir, "std.csv"), sb.ToString());
            }

            // html report
            {
                string case_result_dir = combine(case_dir, "out");

                string dirname = System.IO.Path.GetFileName(case_dir);
                string date = dirname.Split('_')[0];
                string time = dirname.Split('_')[1];
                string user = dirname.Split('_')[2];

                // read template 
                string html_template_file = global_variables.machine_param.get_value("html_report_template");
                if (!System.IO.File.Exists(html_template_file))
                {
                    global_variables.log_error("report template not found: " + html_template_file);
                    return;
                }

                string html = System.IO.File.ReadAllText(html_template_file);

                string report_title = global_variables.machine_param.get_value("report_title");

                html = html.Replace("{{{date}}}", date)
                    .Replace("{{{time}}}", time)
                    .Replace("{{{user}}}", user)
                    .Replace("{{{title}}}", report_title);

                //double point_to_point_dist_tol = System.Convert.ToDouble(global_variables.machine_param.get_value("point_to_point_dist_tol"));
                //double dist_tol = System.Convert.ToDouble(global_variables.machine_param.get_value("dist_tol"));

                html = html
                        .Replace("{{{uniformity_HU_tol}}}", global_variables.machine_param.get_value("uniformity_HU_tol"));

                html = html
                        .Replace("{{{mean_rows}}}", gen_html_table_rows_from_csv(case_result_dir, "mean.csv"))
                        .Replace("{{{std_rows}}}", gen_html_table_rows_from_csv(case_result_dir, "std.csv"));

                // save the report
                string html_file = combine(case_result_dir, "report.html");
                System.IO.File.WriteAllText(html_file, html);
            }

            ///////////////////////
            ////// email the report
            //////case_dir = @"W:\RadOnc\Planning\Physics QA\CTQA\GECTSH\cases\20190412_111928";
            //global_variables.log_line("emailing report...");
            //email_report(case_dir);

            global_variables.log_line("exiting geo_check_markers.run()...");
        }

        double[] calculate_plane_normal_from_three_points(List<double[]> points)
        {
            double[] pt0 = points[0];
            double[] pt1 = points[1];
            double[] pt2 = points[2];

            double[] v1 = sub(pt1, pt0);
            double[] v2 = sub(pt2, pt0);

            double[] norm = cross(v1, v2);

            return unit_vector(norm);
        }

        double[] unit_vector(double[] v)
        {
            double length = norm(v);
            return div(v, length);
        }


        double norm(double[] values)
        {
            return Math.Sqrt(squared_sum(values));
        }

        double angle_between_vectors_deg(double[] v1, double[] v2)
        {
            double y = norm(v1) * norm(v2) / dot(v1, v2);
            // y value cannot be larger than 1.0
            if (y > 1.0)
                return 0;

            double th = Math.Acos(y);
            return th * 180 / Math.PI;
        }

        double squared_sum(double[] values)
        {
            double sum = 0.0;
            foreach (double v in values)
                sum += v * v;
            return sum;
        }


        double[] calculate_plane_normal_from_points(List<double[]> points)
        {
            List<double[]> vectors = new List<double[]>();
            for (int i = 0; i < points.Count(); i++)
            {
                for (int j = 0; j < points.Count(); j++)
                {
                    if (j == i)
                        continue;

                    double[] v = sub(points[j], points[i]);

                    vectors.Add(v);
                }
            }
            return calculate_plane_normal_from_vectors(vectors);
        }

        double[] calculate_plane_normal_from_vectors(List<double[]> vectors)
        {
            List<double[]> cross_vectors = new List<double[]>();
            for (int i = 0; i < vectors.Count(); i++)
            {
                for (int j = 0; j < vectors.Count(); j++)
                {
                    if (j == i)
                        continue;

                    double[] cross_v = cross(vectors[j], vectors[i]);

                    cross_vectors.Add(cross_v);
                }
            }

            return mean_vector(cross_vectors);
        }

        double[] mean_vector(List<double[]> vectors)
        {
            double[] mean = new double[3];
            for (int c = 0; c < 3; c++)
            {
                double sum = 0.0;
                for (int v = 0; v < vectors.Count; v++)
                {
                    sum += vectors[v][c];
                }

                mean[c] = sum / vectors.Count;
            }

            return mean;
        }

        public void email_report(string case_dir)
        {
            string html_file = System.IO.Path.Combine(case_dir, @"out\report.html");

            param p = global_variables.machine_param;
            string from = p.get_value("email_from");
            string to = p.get_value("email_to");
            string from_enc_pw = p.get_value("email_from_enc_pw");
            string body = System.IO.File.ReadAllText(html_file);
            string domain = p.get_value("email_domain");
            string host = p.get_value("email_host_address");
            int port = System.Convert.ToInt32(p.get_value("email_host_port"));
            bool enable_ssl = System.Convert.ToBoolean(p.get_value("enable_ssl"));
            //send
            email.send(from, from_enc_pw, to, "CTQA (" + p.get_value("machine") + ")", body, domain, host, port, enable_ssl);
        }

        string gen_html_table_rows_from_csv(string case_result_dir, string filename, string num_format = "0.0")
        {
            string file = combine(case_result_dir, filename);

            // add rows
            StringBuilder sb = new StringBuilder();

            string[] lines = System.IO.File.ReadAllLines(file);

            // header line
            {
                sb.AppendLine("<tr>");
                string line = lines[0];
                foreach (string value in line.Split(','))
                    sb.AppendLine("<th>" + value + "</th>");
                sb.AppendLine("</tr>");
            }

            // rows
            for (int i = 1; i < lines.Length; i++)
            {

                string line = lines[i];

                if (line.ToLower().Contains("fail"))
                    sb.AppendLine("<tr class='fail'>");
                else
                    sb.AppendLine("<tr>");

                foreach (string value in line.Split(','))
                    sb.AppendLine("<td>" + value + "</td>");
                sb.AppendLine("</tr>");
            }
            return sb.ToString();
        }


        void analize(string case_dir, string out_dir)
        {
            // get sub folders
            string[] dirs = System.IO.Directory.GetDirectories(case_dir);
            foreach(string dir in dirs)
            {
                string dirname = System.IO.Path.GetFileName(dir);

                if (dir == "out")
                    continue;

                analize_CT(dir);
            }
        }

        void analize_CT(string img_dir)
        {
            global_variables.log_line("analize_CT()");
            global_variables.log_line("img_dir="+img_dir);

            //phantom is body or largebody
            string img_dirname = System.IO.Path.GetFileName(img_dir);
            string[] elms = img_dirname.Split('-');
            if (elms.Length != 3)
                return;
            string phantom = elms[0].Trim().ToLower();
            global_variables.log_line("phantom=" + phantom);

            string image = global_variables.combine(img_dir, "CT.mhd");

            // convert to mhd
            dicomtools.dicom_series_to_mhd(img_dir, img_dir);

            // dirs
            string case_dir = System.IO.Path.GetDirectoryName(img_dir);
            string cases_dir = System.IO.Path.GetDirectoryName(case_dir);
            string machine_dir = System.IO.Path.GetDirectoryName(cases_dir);

            
            {
                string masks_dir = global_variables.combine(machine_dir, "masks");
                string mask_dir = global_variables.combine(masks_dir, phantom);

                string[] mask_names = { "center", "north", "south", "east", "west" };
                List<double> means = new List<double>();
                List<double> stds = new List<double>();

                StringBuilder sb_csv = new StringBuilder();
                sb_csv.AppendLine("loc,mean, std");

                StringBuilder sb_txt = new StringBuilder();

                foreach (string mask_name in mask_names)
                {
                    string mask = global_variables.combine(mask_dir, mask_name + ".nrrd");
                    string out_file = global_variables.combine(img_dir, mask_name + ".txt");
                    imagetools.calc_image_min_max_mean_std_3d_f(image, mask, out_file);
                    param p = new param(out_file);
                    double mean = System.Convert.ToDouble(p.get_value("mean"));
                    double std = System.Convert.ToDouble(p.get_value("std"));

                    means.Add(mean);
                    stds.Add(std);

                    sb_csv.AppendLine(string.Format("{0},{1},{2}", mask_name, mean, std));
                    sb_txt.AppendLine(string.Format("{0}_mean_std={1},{2}", mask_name, mean, std));
                }

                // max diff is the uniformity
                List<double> diffs = new List<double>();
                double max_diff = 0.0;
                for(int i=1; i<5; i++)
                {
                    double diff = System.Math.Abs(means[0] - means[i]);
                    if (diff > max_diff)
                        max_diff = diff;
                }
                global_variables.log("max_diff = " + max_diff.ToString());
                double uniformity_HU_tol = System.Convert.ToDouble(global_variables.machine_param.get_value("uniformity_HU_tol"));
                sb_csv.AppendLine(string.Format("max diff from center,{0}", max_diff));
                sb_txt.AppendLine(string.Format("max diff from center={0}", max_diff));

                // mean of the standard diviation
                double avg_std = 0.0;
                for (int i = 0; i < 5; i++)
                {
                    avg_std += stds[i];
                }
                avg_std = avg_std / 5.0;
                global_variables.log("avg_of_stds = " + avg_std);
                sb_csv.AppendLine(string.Format("avg of STDs,,{0}", avg_std));
                sb_txt.AppendLine(string.Format("avg of STDs={0}", avg_std));

                // save to a file
                string result_csv_file = global_variables.combine(img_dir, "mean_std.csv");
                global_variables.log("saving results to "+ result_csv_file);
                System.IO.File.WriteAllText(result_csv_file, sb_csv.ToString());

                string result_txt_file = global_variables.combine(img_dir, "mean_std.txt");
                global_variables.log("saving results to " + result_txt_file);
                System.IO.File.WriteAllText(result_txt_file, sb_txt.ToString());


            }

        }

        string toString(string value, string num_format)
        {
            return string.Format("{0:" + num_format + "}", System.Convert.ToDouble(value));
        }

        string toString(double[] values, string num_format)
        {
            List<string> list = new List<string>();
            foreach (double v in values)
                list.Add(string.Format("{0:" + num_format + "}", v));

            return string.Join(",", list);
        }

        double dist(double[] pt1, double[] pt2)
        {
            double[] d = sub(pt1, pt2);
            return Math.Sqrt(d[0] * d[0] + d[1] * d[1] + d[2] * d[2]);
        }

        double[] cross(double[] a, double[] b)
        {
            double[] c = new double[3];

            c[0] = a[1] * b[2] - a[2] * b[1];
            c[1] = a[2] * b[0] - a[0] * b[2];
            c[2] = a[0] * b[1] - a[1] * b[0];

            return c;
        }

        double dot(double[] a, double[] b)
        {
            double sum = 0.0;
            for (int i = 0; i < a.Length; i++)
                sum += a[i] * b[i];

            return sum;
        }


        double[] toDouble(string[] values)
        {
            List<double> list = new List<double>();
            foreach (string s in values)
                list.Add(System.Convert.ToDouble(s));

            return list.ToArray();
        }

        double min(double[] values)
        {
            double min = double.MaxValue;
            foreach (double v in values)
            {
                if (v < min)
                    min = v;
            }
            return min;
        }

        double max(double[] values)
        {
            double max = double.MinValue;
            foreach (double v in values)
            {
                if (v > max)
                    max = v;
            }
            return max;
        }

        double mean(double[] values)
        {
            double sum = 0.0;
            foreach (double v in values)
            {
                sum += v;
            }
            return (sum / values.Length);
        }

        double std(double[] values)
        {
            double m = mean(values);

            double sum = 0.0;
            foreach (double v in values)
            {
                sum += (v - m) * (v - m);
            }

            return Math.Sqrt(sum / values.Length);
        }

        int[] toInt(double[] values)
        {
            List<int> list = new List<int>();
            foreach (double v in values)
                list.Add(System.Convert.ToInt32(v));

            return list.ToArray();
        }


        string[] toString(double[] values)
        {
            List<string> list = new List<string>();
            foreach (double s in values)
                list.Add(s.ToString());

            return list.ToArray();
        }

        double[] div(double[] values, double denom)
        {
            List<double> list = new List<double>();
            foreach (double v in values)
                list.Add(v / denom);

            return list.ToArray();
        }

        double[] add(double[] values, double a)
        {
            List<double> list = new List<double>();
            foreach (double v in values)
                list.Add(v + a);

            return list.ToArray();
        }

        double[] sub(double[] values, double a)
        {
            List<double> list = new List<double>();
            foreach (double v in values)
                list.Add(v - a);

            return list.ToArray();
        }


        double[] add(double[] values1, double[] values2)
        {
            List<double> list = new List<double>();
            for (int i = 0; i < values1.Length; i++)
                list.Add(values1[i] + values2[i]);

            return list.ToArray();
        }

        double[] sub(double[] values1, double[] values2)
        {
            List<double> list = new List<double>();
            for (int i = 0; i < values1.Length; i++)
                list.Add(values1[i] - values2[i]);

            return list.ToArray();
        }

        double[] div(double[] values1, double[] values2)
        {
            List<double> list = new List<double>();
            for (int i = 0; i < values1.Length; i++)
                list.Add(values1[i] / values2[i]);

            return list.ToArray();
        }

        int[] sub(int[] values1, int[] values2)
        {
            List<int> list = new List<int>();
            for (int i = 0; i < values1.Length; i++)
                list.Add(values1[i] - values2[i]);

            return list.ToArray();
        }




    }
}
