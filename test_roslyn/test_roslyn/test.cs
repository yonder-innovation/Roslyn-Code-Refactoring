using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace test_roslyn
{
    class test
    {        
        protected int number;

        public test(int n)
        {
            number = n;
        }

        public test()
        {
            number = 0;
        }

        public int getNumber()
        {
            return number;
        }

    }

    class test1 : test
    {
        private string s;
		private long l;

        public long L
        {
            get { return l; }
            set { l = value; }
        }

        public test1(string s, long l, int n)
        {
            this.s = s;
            this.l = l;
            this.number = n;
        }
    }

}
