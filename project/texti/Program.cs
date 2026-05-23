namespace textiMain;

using System.Text;

class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding  = Encoding.UTF8;

        DisplayLogo();

        string? path;

        if (args.Length > 0)
        {
            path = args[0];
        }
        else
        {
            Console.Write("  Укажите название файла или его путь: ");
            path = Console.ReadLine();
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            Console.WriteLine("\n  Не указан путь!");
            return;
        }

        var editor = new Editor(path);
        editor.Run();
    }

    static void DisplayLogo()
    {
        string logo =
 """
     _________ ______ _    _ _________  o
     |___ ___| |  . | \\  // |___ ___| ___
        | |    | ___|  \\//     | |    | |
        | |    | |__   //\\     | |    | |
        |_|    |____| //  \\    |_|    |_|

               текстовый редактор
         
               
     """;
        Console.WriteLine(logo);
    }
}
