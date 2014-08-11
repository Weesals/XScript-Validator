using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using XScript.StackBasedVM;
using RTS4.Environment;
using RTS4.Environment.XScript.RMS;
using XScript.Parser2;
using System.Diagnostics;

namespace XScriptValidityCheck {
    public static class Program {

        static void Main(string[] args) {
            for (int a = 0; a < args.Length; ++a) {
                string file = args[a];
                if (!File.Exists(file)) {
                    Console.Error.WriteLine("File `" + file + "` could not be found.");
                } else {
                    CompileFile(file);
                }
            }
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey(true);
        }

        public static void CompileFile(string name) {
            using (var file = File.Open(name, FileMode.Open))
            using (var reader = new StreamReader(file)) {
                Console.WriteLine("===Finding file `" + Path.GetFileName(name) + "`===");
                Console.Write("Reading file... ");
                string data = reader.ReadToEnd();
                Console.WriteLine(data.Length + " bytes");
                Console.WriteLine("Parsing file...");
                var instructions = Parser.Parse(data, delegate(string message, Parser.ErrorLevelE level, int line) {
                    switch (level) {
                        case Parser.ErrorLevelE.Info: Console.WriteLine("Info(" + line + "): " + message); break;
                        case Parser.ErrorLevelE.Warning: Console.WriteLine("Warning(" + line + "): " + message); break;
                        case Parser.ErrorLevelE.Error: Console.Error.WriteLine("Error(" + line + "): " + message); break;
                    }
                });
                Console.WriteLine("===Setting up simulation environment===");
                var runtime = new VirtualMachine.Runtime(instructions);
                runtime.PushBlock();
                var simulation = new Simulation();
                var world = new World(simulation);
                var bag = new XRandomMap((int)DateTime.Now.Ticks, simulation);
                runtime.AddMethodsFrom(new XMath());
                runtime.AddMethodsFrom(new XLogging());
                runtime.AddMethodsFrom(bag);
                runtime.ReadConstantsFrom("constants.xml");
                runtime.AddPropertiesFrom(bag);
                Console.WriteLine("===Checking functions===");
                List<string> variables = new List<string>();
                for (int i = 0; i < instructions.Length; ++i) {
                    var instr = instructions[i];
                    if (instr.Instruction.Name == "Allocate") {
                        if (!(instr.Data is string)) Console.WriteLine("Internal error, variable name cannot be found!");
                        else variables.Add((string)instr.Data);
                    }
                    if (instr.Instruction.Name.StartsWith("Call")) {
                        if (!(instr.Data is string)) Console.WriteLine("Internal error, function name cannot be found!");
                        else {
                            string fnName = (string)instr.Data;
                            if (!variables.Contains(fnName) && !runtime.FunctionExists(fnName)) {
                                Console.Error.WriteLine("Simulation Error: Unable to find function " + fnName);
                            }
                        }
                    }
                }
                Console.WriteLine("===Running script===");
                while (!runtime.Complete) runtime.Step();
                runtime.Invoke("main", 0);
                while (!runtime.Complete) runtime.Step();
                Console.WriteLine("===Testing done===");
            }
        }

    }
}
