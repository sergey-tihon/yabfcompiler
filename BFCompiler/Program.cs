﻿
namespace YABFcompiler
{
    using System;
    using System.IO;
    using CommandLineArgs;
    using Exceptions;
    using LanguageParsers;

    internal class Program
    {
        private static OptionSet options;

        private static string option_filename;
        private static CompilationOptions option_compilationOptions = 0;

        static void Main(string[] args)
        {
            if (!HandleCommandLineArgs(args))
            {
                ShowHelp(options);
                return;
            }

            var compiler = GetCompiler(option_filename);

            compiler.Compile(Path.GetFileNameWithoutExtension(option_filename));
        }

        private static Compiler GetCompiler(string filename)
        {
            var code = ReadFile(filename);
            var fileInfo = new FileInfo(filename);
            Parser parser;

            switch (fileInfo.Extension.Substring(1).ToLower()) // remove the period.
            {
                case "bf": parser = new BrainfuckParser(); break;
                case "ook": parser = new OokParser(); break;
                case "sook": parser = new ShortOokParser(); break;
                default: throw new UnknownLanguageException();
            }

            return new Compiler(parser.GenerateDIL(code), option_compilationOptions);

        }

        private static void ShowHelp(OptionSet options)
        {
            Console.WriteLine("Usage: {0} [options] <source>\n", AppDomain.CurrentDomain.FriendlyName);
            Console.WriteLine("Options:");
            options.WriteOptionDescriptions(Console.Out);
        }

        private static string ReadFile(string file)
        {
            return File.ReadAllText(file);
        }

        private static bool HandleCommandLineArgs(string[] args)
        {
            bool status = true;
            options = new OptionSet
                          {
                              {"d", "Debug mode", v => option_compilationOptions |= CompilationOptions.DebugMode},
                              {"?|h|help", "Show help", v => { status = false; }},
                              {"<>", v => option_filename = v}
                          };
            try
            {
                options.Parse(args);
            }
            catch (OptionException)
            {
                status = false;
            }

            if (String.IsNullOrEmpty(option_filename))
            {
                status = false;
            }

            return status;
        }
    }
}
