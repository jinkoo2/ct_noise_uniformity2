using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ctqa_mpc_cmd
{
    class Program
    {
        static void Main(string[] args)
        {
            
            /////////////////
            // command line
            if (true)
            {
                string service_param_file = @"W:\RadOnc\Planning\Physics QA\CTQA\NoiseUniformity\service_param.txt";
                string machine_param_file = @"W:\RadOnc\Planning\Physics QA\CTQA\NoiseUniformity\GECTSH\machine_param.txt";
                string case_dir = @"W:\RadOnc\Planning\Physics QA\CTQA\NoiseUniformity\GECTSH\cases\20220411_000000_JK";
                (new ct_noise_uniformity_lib.app()).run(case_dir, service_param_file, machine_param_file);
            }
            else
            {
                // //////////////////////
                // // watcher
                // string param_file = @"\\uhmc-fs-share\shares\RadOnc\Planning\Physics QA\CTQA\NoiseUniformity\service_param.txt";
                // ct_noise_uniformity_lib.fswatcher watcher = new ct_noise_uniformity_lib.fswatcher(param_file);
                // bool watch_mode = false;
                // if (watch_mode)
                // {
                //     watcher.start_watching();
                //     Console.WriteLine("Hit Enter to quit...");
                //     Console.ReadLine();
                // }
                // else
                // {
                //     // run from import (bypassing watching step).
                //     string import_dir = @"\\rovelocity\import\Noise _Uniformity";
                //     watcher.Run(import_dir);
                // }
            }
        }
    }
}
