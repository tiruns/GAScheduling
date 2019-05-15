using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GAScheduling
{
    public interface IHasId
    {
        int Id { get; }
    }

    public class RequireAutoId : IHasId
    {
        public int Id { get; }

        private static Dictionary<Type, int> NextId = new Dictionary<Type, int>();

        public RequireAutoId()
        {
            if (!NextId.ContainsKey(this.GetType()))
                NextId.Add(this.GetType(), 0);
            this.Id = NextId[GetType()]++;
        }
    }

    public class Pair
    {
        public string Key { get; set; }
        public string Value { get; set; }

        public void Parse(List<string> elems)
        {
            if (elems.Count != 2)
                throw new Exception("Invalid Pair Item.");

            this.Key = elems[0];
            this.Value = elems[1];
        }
    }
}
