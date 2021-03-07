using Casasoft.Sillogismi;
using System;

Console.WriteLine("Casasoft Sillogismi");
KnowledgeBase kb = new();

string ret = string.Empty;
while (ret != kb.Goodbye)
{
    Console.Write("> ");
    string sentence = Console.ReadLine();
    ret = kb.Process(sentence);
    Console.WriteLine(ret);
}
 