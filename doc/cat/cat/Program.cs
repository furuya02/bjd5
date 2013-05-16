using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Linq;

namespace cat{
    internal class Program{
        static void Main(string[] args) {
            TextReader input;
            input = Console.In;
            Proc(input);
            input.Dispose();
        }

        static void Proc(TextReader tr) {
            Console.WriteLine(tr.ReadToEnd());
            return;
            string line;
            while ((line = tr.ReadLine()) != null) {
                Console.WriteLine(line);
            }
        }

       
    }
}
