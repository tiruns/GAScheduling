using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace GAScheduling
{
    public class Course : IHasId
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int PeriodsPerWeek { get; set; }
        public bool IsOptional { get; set; }
    }

    public class Teacher : IHasId
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<int> ProvidedCourse { get; set; }
    }

    public class Room : IHasId
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Capacity { get; set; }
    }

    public class Class : IHasId
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int NumberOfStudents { get; set; }
        public List<int> RequiredCourses { get; set; }
    }

    public class SubClass : RequireAutoId
    {
        public int RequiredCourses { get; set; }
        public int NumberOfStudents { get; set; }
        public List<int> DerivingFrom { get; set; }
    }

    public class Datasets
    {
        private static Datasets mDatasets;

        public static Datasets Get()
        {
            if (mDatasets == null)
                mDatasets = new Datasets();
            return mDatasets;
        }

        public List<Course> Courses { get; set; }
        public List<Teacher> Teachers { get; set; }
        public List<Room> Rooms { get; set; }
        public List<Class> Classes { get; set; }
        public List<SubClass> SubClasses { get; set; }
        public int TotalPeriods { get; set; }

        private readonly string mRoot;

        private Datasets()
        {
            if (mDatasets != null)
                throw new Exception("There should be only one instance of class Datasets.");
            mDatasets = this;

            var root = Config.DatasetsRoot;
            if (!root.EndsWith("/") && !root.EndsWith("\\"))
                root += System.IO.Path.DirectorySeparatorChar;
            mRoot = root;

            try
            {
                LoadAll();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error occured when loading datasets:");
                throw ex;
            }
        }

        private void LoadAll()
        {
            this.TotalPeriods = Config.PeriodsPerDay * Config.WorkDaysPerWeek;

            this.Courses = CsvReader.ReadAll<Course>(mRoot + "Courses.csv", elems =>
            {
                return new Course()
                {
                    Id = int.Parse(elems[0]),
                    Name = elems[1],
                    PeriodsPerWeek = int.Parse(elems[2]),
                    IsOptional = bool.Parse(elems[3])
                };
            });
            this.Teachers = CsvReader.ReadAll<Teacher>(mRoot + "Teachers.csv", elems =>
            {
                return new Teacher()
                {
                    Id = int.Parse(elems[0]),
                    Name = elems[1],
                    ProvidedCourse = MapIndices<Course>(elems, 2, name =>
                    {
                        return Courses.Find(c => c.Name == name);
                    })
                };
            });
            this.Rooms = CsvReader.ReadAll<Room>(mRoot + "Rooms.csv", elems =>
            {
                return new Room()
                {
                    Id = int.Parse(elems[0]),
                    Name = elems[1],
                    Capacity = int.Parse(elems[2])
                };
            });
            this.Classes = CsvReader.ReadAll<Class>(mRoot + "Classes.csv", elems =>
            {
                return new Class()
                {
                    Id = int.Parse(elems[0]),
                    Name = elems[1],
                    NumberOfStudents = int.Parse(elems[2]),
                    RequiredCourses = MapIndices<Course>(elems, 3, name =>
                    {
                        return Courses.Find(c => c.Name == name);
                    })
                };
            });
            this.SubClasses = CsvReader.ReadAll<SubClass>(mRoot + "SubClasses.csv", elems =>
            {
                var course = Courses.Find(c => c.Name == elems[0]);
                if (course == null)
                    throw new Exception("Invalid item:" + elems[0]);

                return new SubClass()
                {
                    RequiredCourses = course.Id,
                    NumberOfStudents = int.Parse(elems[1]),
                    DerivingFrom = MapIndices<Class>(elems, 2, name =>
                    {
                        return Classes.Find(c => c.Name == name);
                    })
                };
            });
        }

        private List<int> MapIndices<T>(List<string> elems, int offset, Func<string, T> map) where T : IHasId
        {
            var list = new List<int>();
            for (int i = offset; i < elems.Count; ++i)
            {
                if (elems[i] == "")
                    continue;

                var item = map(elems[i]);
                if (item != null)
                    list.Add(item.Id);
                else
                    throw new Exception("Invalid item: " + elems[i]);
            }

            return list;
        }
    }
}
