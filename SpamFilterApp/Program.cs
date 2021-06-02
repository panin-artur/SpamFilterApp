using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpamFilterLibrary;
using System.Net;
using System.Text.RegularExpressions;

namespace SpamFilterApp
{
    class Program
    {
        static void Main(string[] args)
        {
            SpamFilter oSF = new SpamFilter();
            string szLine;

            TextReader oLearnFile = File.OpenText(@"Data\LearnData.txt");
            while ((szLine = oLearnFile.ReadLine()) != null)
            {
                oSF.Learn(szLine.Substring(szLine.IndexOf(',') + 1), szLine.ToLower().StartsWith("spam,") ? MessageType.Spam : MessageType.Ham);
            }
            oLearnFile.Close();
            oSF.Transparent();

            Console.WriteLine("Вывести частотный словарь?");
            string TextRead_lib = Console.ReadLine();
            if (TextRead_lib == "да" || TextRead_lib == "Да" || TextRead_lib == "lf")
            {
                ///подсчёт числа слов
                var rowsInLine = 60;
                var dict = new Dictionary<string, List<int>>();
                string[] line; //объявляем массив слов
                int countOfRows = 0;
                using (var sr = new StreamReader(@"Data\TestData.txt")) //создаем стримридер
                    while (!sr.EndOfStream) //пока наш экземпляр ридера не достиг конца потока
                    {
                        line = sr.ReadLine().ToLower().Split(" < > <? >? +:=1234567890 ''%-<><p></p>,.?!\'()\"".ToCharArray(), StringSplitOptions.RemoveEmptyEntries); //считываем строку из потока, переводим в нижний регистр, разбиваем на отдельные слова по массиву разделителей и убираем пустые вхождения
                        countOfRows++;
                        foreach (var word in line) //перебираем весь массив слов
                        {
                            if (!dict.ContainsKey(word)) dict.Add(word, new List<int>() { countOfRows / rowsInLine + 1 }); //если в словаре нет такого ключа, добавляем в словарь новую пару, ключ слово, значение новый список из одного эл-та - номера страницы...хотя, думаю, лучше все же номера строк заносить, потом можно будет переделать вывод на строки,абзацы,страницы,тома и т.д.:)
                            else dict[word].Add(countOfRows / rowsInLine + 1); //если же значение есть, то добавляем в список еще одно значение
                        }
                    }
                var result = dict.OrderBy(x => x.Key).GroupBy(x => x.Key[0]).OrderBy(x => x.Key); //словарь сортируем по ключу(по словам в алфавитном порядке), группируем по первой букве(алфавитный указатель), еще раз сортируем(это излишне, когда писал, перестарался, лучше убрать, не мешает, но и лишняя работа ни к ченму)

                foreach (var group in result) //перебираем последовательность
                {
                    Console.WriteLine(group.Key.ToString().ToUpper()); //выводим ключ группы, в верхнем регистре
                    foreach (var item in group) //перебираем уже саму группу
                    {
                        Console.WriteLine(item.Key.PadRight(20, '.') + " " + item.Value.Count + ":" + string.Join(" ", item.Value.Distinct())); //выводим ключ группы с выравниванием + количество записей в списке List + само содерживое списка через пробел, с удалением дублей
                    }
                }
            }
            else
            {
                Console.WriteLine("Ну ладно");
            }

            Console.WriteLine("Вывести все спам-слова?");
            string TextRead = Console.ReadLine();

            if (TextRead == "да" || TextRead == "Да" || TextRead == "lf")
            {
                string[] lines = File.ReadAllLines(@"Data\1.txt");
                foreach (var line1 in lines)
                {
                    Console.WriteLine(line1);
                }
            }

            else
            {
                Console.WriteLine("Ну ладно");
            }

            FileStream fs = null;
            Console.WriteLine("Осуществляем поиск синонимов, пожалуйста подождите!");

            try
            {
                fs = new FileStream(@"Data\3.txt", FileMode.OpenOrCreate);
                using (StreamWriter fale = new StreamWriter(fs))
                {
                    foreach (String s in oSF._SpamWordsList)
                    {
                        string apiURL1 = "https://rusvectores.org/";
                        string word1 = s;
                        string model1 = "ruwikiruscorpora_upos_skipgram_300_2_2019";
                        string format1 = "json";
                        WebClient client1 = new WebClient();
                        string downloadString1 = client1.DownloadString(apiURL1 + "/" + model1 + "/" + word1 + "/api/" + format1 + "/");

                        Console.WriteLine("Запрашиваем с - " + apiURL1 + "/" + model1 + "/" + word1 + "/api/json/");
                        Console.WriteLine("Ищем синонимы слова - " + word1 + "!");

                        List<String> words = new List<String>();
                        Regex rx = new Regex(@"(\w+)_(\w+)(.?: \d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                        MatchCollection matches = rx.Matches(downloadString1);

                        foreach (Match match in matches)
                        {
                            words.Add(match.Groups[1].Value);
                        }

                        foreach (String w in words)
                        {
                            Console.WriteLine("{0}", w);
                            if (w.Length != 0)
                            {
                                fale.WriteLine(w);
                            }
                        }
                        fale.WriteLine(s);

                        foreach (String w in words)
                        {
                            Console.WriteLine("{0}", w);
                            if (w.Length != 0)
                            {
                                fale.WriteLine(w);
                                oSF.Learn(w, MessageType.Spam);
                            }
                        }
                        fale.WriteLine(s);
                        oSF.Learn(s, MessageType.Spam);
                    }
                    TextReader oTestFile = File.OpenText(@"Data\TestData.txt");
                    while ((szLine = oTestFile.ReadLine()) != null)
                    {
                        MessageType eExpectedType = szLine.ToLower().StartsWith("spam,") ? MessageType.Spam : MessageType.Ham;
                        MessageType eType = oSF.Classify(szLine.Substring(szLine.IndexOf(',') + 1));

                        if (eExpectedType != eType)
                        {
                            Console.Write("!Спам - {0}{1}", szLine.Substring(0, 35) + "...", Environment.NewLine);
                        }
                        else
                        {
                            Console.Write("Спам - \t{0}{1}", szLine.Substring(0, 35) + "...", Environment.NewLine);
                        }
                    }

                }
                Console.ReadKey(true);
            }
            catch
            {
                Console.WriteLine("Что-то пошло не так, проверьте файлы!");
            }
        }
    }
}