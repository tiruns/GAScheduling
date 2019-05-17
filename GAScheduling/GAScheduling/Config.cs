using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GAScheduling
{
    class Config
    {
        public static string DatasetsRoot = @"./Datasets/";

        public static int PopulationSize = 640;
        public static int Iterations = 6400;
        public static float MutationRate = 0.005f;
        public static bool UseDynamicMutationRate = true;
        public static float DecayRate = 0.75f;
        public static int PeriodsPerDay = 8;
        public static int WorkDaysPerWeek = 5;
    }
}
