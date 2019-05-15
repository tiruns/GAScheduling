using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GAScheduling
{
    public class DNA : IComparable<DNA>
    {
        public int RoomId { get; set; }
        public int TimeId { get; set; }
        public int TeacherId { get; set; }

        public DNA()
        {
            var datasets = Datasets.Get();

            this.RoomId = datasets.Rooms[RandomEngine.Next(datasets.Rooms.Count)].Id;
            this.TeacherId = datasets.Teachers[RandomEngine.Next(datasets.Teachers.Count)].Id;
            this.TimeId = RandomEngine.Next(datasets.TotalPeriods);

        }

        public DNA(DNA left, DNA right)
        {
            this.RoomId = RandomEngine.NextDouble() < 0.5 ? left.RoomId : right.RoomId;
            this.TeacherId = RandomEngine.NextDouble() < 0.5 ? left.TeacherId : right.TeacherId;
            this.TimeId = RandomEngine.NextDouble() < 0.5 ? left.TimeId : right.TimeId;

            var datasets = Datasets.Get();

            if (RandomEngine.NextDouble() < Config.MutationRate)
            {
                if (RandomEngine.NextDouble() < 0.33)
                    this.RoomId = datasets.Rooms[RandomEngine.Next(datasets.Rooms.Count)].Id;
                if (RandomEngine.NextDouble() < 0.33)
                    this.TeacherId = datasets.Teachers[RandomEngine.Next(datasets.Teachers.Count)].Id;
                if (RandomEngine.NextDouble() < 0.33)
                    this.TimeId = RandomEngine.Next(datasets.TotalPeriods);
            }
        }

        public int CompareTo(DNA other)
        {
            return this.TimeId - other.TimeId;
        }
    }

    public class Chromosome : IComparable<Chromosome>
    {
        public int ClassId { get; }
        public bool IsSubClass { get; }
        public int CourseId { get; }

        public DNA Gene { get; set; }

        private Chromosome(int courseId)
        {
            this.CourseId = courseId;
            this.Gene = new DNA();
        }

        public Chromosome(Class cls, int courseId) :
            this(courseId)
        {
            this.ClassId = cls.Id;
            this.IsSubClass = false;
        }

        public Chromosome(SubClass subClss, int courseId) :
            this(courseId)
        {
            this.ClassId = subClss.Id;
            this.IsSubClass = true;
        }

        public Chromosome(Chromosome clone) :
            this(clone.CourseId)
        {
            this.IsSubClass = clone.IsSubClass;
            this.ClassId = clone.ClassId;
        }

        public Chromosome(Chromosome left, Chromosome right)
        {
            if (left.IsSubClass != right.IsSubClass ||
                left.ClassId != right.ClassId ||
                left.CourseId != right.CourseId)
                throw new Exception("Chromosome mismatch.");

            this.IsSubClass = left.IsSubClass;
            this.CourseId = left.CourseId;
            this.ClassId = left.ClassId;

            this.Gene = new DNA(left.Gene, right.Gene);
        }

        public int CompareTo(Chromosome other)
        {
            return this.Gene.CompareTo(other.Gene);
        }
    }

    public class Individual
    {
        public List<Chromosome> Genome { get; }

        private float? mFitness;

        public float Fitness
        {
            get
            {
                if (!mFitness.HasValue)
                    mFitness = new Evaluator(this).Evaluate();
                return mFitness.GetValueOrDefault();
            }
        }

        public Individual()
        {
            this.Genome = new List<Chromosome>();
            var datasets = Datasets.Get();

            foreach (var cls in datasets.Classes)
            {
                foreach (var course in cls.RequiredCourses)
                {
                    int periods = datasets.Courses.Find(c => c.Id == course).PeriodsPerWeek;
                    for (int i = 0; i < periods; ++i)
                        this.Genome.Add(new Chromosome(cls, course));
                }
            }
            foreach (var sub in datasets.SubClasses)
            {
                int periods = datasets.Courses.Find(c => c.Id == sub.RequiredCourses).PeriodsPerWeek;
                for (int i = 0; i < periods; ++i)
                    this.Genome.Add(new Chromosome(sub, sub.RequiredCourses));
            }
        }

        public Individual(Individual clone)
        {
            this.Genome = new List<Chromosome>();
            foreach (var gene in clone.Genome)
                this.Genome.Add(new Chromosome(gene));
        }

        public Individual(Individual left, Individual right)
        {
            this.Genome = new List<Chromosome>();
            if (left.Genome.Count != right.Genome.Count)
                throw new Exception("Different species.");

            for (int i=0; i< left.Genome.Count; ++i)
            {
                this.Genome.Add(new Chromosome(left.Genome[i], right.Genome[i]));
            }
        }
    }

    public class Population
    {
        public int Generation { get; }
        public List<Individual> Individuals { get; }

        private float? mAverageFitness;
        private float? mMaxFitness;

        public float AverateFitness
        {
            get
            {
                if (!mAverageFitness.HasValue)
                {
                    var sum = 0.0f;
                    foreach (var individual in this.Individuals)
                        sum += individual.Fitness;
                    mAverageFitness = sum / this.Individuals.Count;
                }
                return mAverageFitness.GetValueOrDefault();
            }
        }

        public float MaxFitness
        {
            get
            {
                if (!mMaxFitness.HasValue)
                {
                    mMaxFitness = this.Individuals.Max(i => i.Fitness);
                }
                return mMaxFitness.GetValueOrDefault();
            }
        }

        public Population()
        {
            this.Generation = 0;
            this.Individuals = new List<Individual>();
            var seed = new Individual();
            this.Individuals.Add(seed);
            for (int i = 1; i < Config.PopulationSize; ++i)
                this.Individuals.Add(new Individual(seed));
        }

        public Population(Population lastGen)
        {
            this.Generation = lastGen.Generation + 1;
            this.Individuals = new List<Individual>();

            var breakpoints = new List<float>();

            float sum = 0;

            foreach (var individual in lastGen.Individuals)
            {
                sum += individual.Fitness;
                breakpoints.Add(sum);
            }

            for (int i = 1; i < Config.PopulationSize; ++i)
            {
                Func<int> select = () =>
                {
                    var pos = RandomEngine.NextDouble() * sum;
                    for (int j = 0; j < breakpoints.Count; ++j)
                    {
                        if (pos <= breakpoints[j])
                            return j;
                    }
                    return -1;
                };

                var left = lastGen.Individuals[select()];
                var right = lastGen.Individuals[select()];

                this.Individuals.Add(new Individual(left, right));
            }
        }

        public override string ToString()
        {
            var max = (new Evaluator(this.Individuals[0])).Max();
            var avgPercentage = this.AverateFitness / max;
            var maxPercentage = this.MaxFitness / max;

            var builder = new StringBuilder();
            builder.Append("Generation: ");
            builder.Append(string.Format("{0:00000}", this.Generation));

            builder.Append("\tAverage Fitness: ");
            builder.Append(string.Format("{0:.00}", this.AverateFitness));
            builder.Append(string.Format(" ({0:.00}%)", avgPercentage * 100));

            builder.Append("\tMax Fitness: ");
            builder.Append(string.Format("{0:.00}", this.MaxFitness));
            builder.Append(string.Format(" ({0:.00}%)", maxPercentage * 100));

            return builder.ToString();
        }
    }

    public class Solver
    {
        private float UpdateMutationRate(float initial, int generation)
        {
            var progress = Convert.ToSingle(generation) / Config.Iterations * Config.DecayRate;
            Config.MutationRate =  Convert.ToSingle((1 - progress) * (1 - progress)) * initial;
            return Config.MutationRate;
        }

        public void Run()
        {
            var pop = new Population();
            Console.WriteLine(pop);

            var mutationRate = Config.MutationRate;
            var maxFitness = (new Evaluator(pop.Individuals[0])).Max();

            for (int i=1; i<Config.Iterations; ++i)
            {
                RandomEngine.NewSeed();
                if (Config.UseDynamicMutationRate)
                    UpdateMutationRate(mutationRate, i);

                pop = new Population(pop);
                Console.WriteLine(pop);

                if (Math.Abs(pop.MaxFitness - maxFitness) < 0.001)
                {
                    break;
                }
            }

            foreach (var individual in pop.Individuals)
            {
                if (Math.Abs(pop.MaxFitness - maxFitness) < 0.001)
                {
                    individual.Genome.Sort();
                    PrintResult(individual);
                    WriteCsv(individual);
                    break;
                }
            }
        }

        public void WriteCsv(Individual individual)
        {
            string path = "";
            for (int i =0; i < 32; ++i)
            {
                path = "result_" + RandomEngine.Next(320) + ".csv";
                if (!System.IO.File.Exists(path))
                {
                    var datasets = Datasets.Get();

                    var header = new List<string>
                    {
                        "DayOfWeek",
                        "Period",
                        "Course",
                        "Room",
                        "Classes",
                        "Teacher"
                    };

                    CsvWriter.WriteAll<Chromosome>(path, header, individual.Genome, chromo =>
                    {
                        var list = new List<string>();

                        var day = chromo.Gene.TimeId / Config.PeriodsPerDay;
                        var period = chromo.Gene.TimeId % Config.PeriodsPerDay;

                        string clsString = "";

                        if (chromo.IsSubClass)
                        {
                            var sub = datasets.SubClasses.Find(c => c.Id == chromo.ClassId).DerivingFrom;
                            foreach (var cid in sub)
                                clsString += datasets.Classes.Find(c => c.Id == cid).Name + " ";
                        }
                        else
                        {
                            clsString = datasets.Classes.Find(c => c.Id == chromo.ClassId).Name;
                        }

                        list.Add(day.ToString());
                        list.Add(period.ToString());
                        list.Add(datasets.Courses.Find(c => c.Id == chromo.CourseId).Name);
                        list.Add(datasets.Rooms.Find(r => r.Id == chromo.Gene.RoomId).Name);
                        list.Add(clsString);
                        list.Add(datasets.Teachers.Find(t => t.Id == chromo.Gene.TeacherId).Name);

                        return list;
                    });

                    Console.WriteLine("File saved: " + path);
                    return;
                }
            }
            throw new Exception("Can't write to csv file: " + path);
        }

        public void PrintResult(Individual individual)
        {
            var datasets = Datasets.Get();

            foreach (var chromo in individual.Genome)
            {
                var day = chromo.Gene.TimeId / Config.PeriodsPerDay;
                var period = chromo.Gene.TimeId % Config.PeriodsPerDay;

                string clsString = "";

                if (chromo.IsSubClass)
                {
                    var sub = datasets.SubClasses.Find(c => c.Id == chromo.ClassId).DerivingFrom;
                    foreach(var cid in sub)
                        clsString += datasets.Classes.Find(c => c.Id == cid).Name + " ";
                }
                else
                {
                    clsString = datasets.Classes.Find(c => c.Id == chromo.ClassId).Name;
                }

                Console.WriteLine("DoW:{0},\tPer:{1},\tCrs:{2},\tRom:{3},\tCls:{4},\tThr:{5}",
                    day,
                    period,
                    datasets.Courses.Find(c => c.Id == chromo.CourseId).Name,
                    datasets.Rooms.Find(r => r.Id == chromo.Gene.RoomId).Name,
                    clsString,
                    datasets.Teachers.Find(t => t.Id == chromo.Gene.TeacherId).Name
                    );
            }
        }
    }
}
