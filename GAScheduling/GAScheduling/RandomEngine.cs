using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace GAScheduling
{
    class RandomEngine
    {
        private static RNGCryptoServiceProvider mSeedGen;
        private static Random mNumberGen;

        static RandomEngine()
        {
            mSeedGen = new RNGCryptoServiceProvider();
            NewSeed();
        }
        
        public static void NewSeed()
        {
            byte[] bytes = new byte[4];
            mSeedGen.GetBytes(bytes);
            mNumberGen = new Random(BitConverter.ToInt32(bytes, 0));
        }

        public static int Next(int maxValue)
        {
            return mNumberGen.Next(maxValue);
        }

        public static double NextDouble()
        {
            return mNumberGen.NextDouble();
        }
    }
}
