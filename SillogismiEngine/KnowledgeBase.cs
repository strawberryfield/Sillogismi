using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Casasoft.Sillogismi
{
    /// <summary>
    /// Gestisce il database delle conoscenze
    /// </summary>
    public class KnowledgeBase
    {
        private Dictionary<string, List<string>> db;

        private string[] QuitCommands = { "FINE", "CIAO", "GRAZIE", "ESCI" };
        private string[] Articoli = { "LLO", "LLA", "LLE", "LL'",
            "IL", "LO", "LA", "I", "GLI", "LE", "UN", "UNO", "UNA", "L'", "UN'", "L" };
        private string[] Predicati = { "E'", "SONO", "è" };
        private string[] QueryCommands = { "PARLAMI DE", "COSA SAI SU", "COSA SAI DE", "COSA SAI DI", "INFORMAZIONI SU" };
        private string[] InverseQueryCommands = { "CHI", "COSA", "CHE COSA" };

        private const string DefaultFilename = "Sillogismi.js";
        private string FileName;

        /// <summary>
        /// Costruttore base
        /// </summary>
        public KnowledgeBase() : this(DefaultFilename)
        {
        }

        /// <summary>
        /// Costruisce il database da un file su disco
        /// </summary>
        /// <param name="filename"></param>
        public KnowledgeBase(string filename)
        {
            FileName = filename;
            if(File.Exists(FileName))
            {
                Load();
            }
            else
            {
                db = new();
            }
        }

        /// <summary>
        /// Frase usata come segnale di conferma fine sessione
        /// </summary>
        public string Goodbye => "Ciao.";

        /// <summary>
        /// Elabora una frase
        /// </summary>
        /// <param name="sentence"></param>
        /// <returns></returns>
        public string Process(string sentence)
        {
            string ret = "Ok.";
            string soggetto = string.Empty;
            string oggetto = string.Empty;

            // assicuriamoci che sia stato scritto qualcosa
            if (string.IsNullOrWhiteSpace(sentence))
                return "Hai scritto qualcosa?";

            // se l'ultimo carattere è un punto lo rimuoviamo
            if (sentence[sentence.Length - 1] == '.' || sentence[sentence.Length - 1] == '?')
                sentence = sentence.Substring(0, sentence.Length - 1);

            sentence = sentence.Trim();

            // Comandi di chiusura sessione
            foreach(string c in QuitCommands)
            {
                if (sentence.StartsWith(c, StringComparison.CurrentCultureIgnoreCase))
                {
                    Save();
                    return Goodbye;
                }
            }

            // Comandi di interrogazione
            foreach (string c in QueryCommands)
            {
                if (sentence.StartsWith(c, StringComparison.CurrentCultureIgnoreCase))
                {
                    List<string> r = Query(RimuoviArticolo(sentence.Substring(c.Length).Trim()));
                    if (r.Count > 0)
                    {
                        return r.Aggregate((i, j) => i + "\n" + j);
                    }
                    else
                    {
                        return "Non so nulla.";
                    }
                }
            }

            // Ricerca del predicato verbale
            foreach (string p in Predicati)
            {
                int pp = sentence.IndexOf(p, StringComparison.CurrentCultureIgnoreCase);
                if (pp > 0)
                {
                    soggetto = sentence.Substring(0, pp).Trim();
                    if (sentence.Length > pp + p.Length)
                        oggetto = sentence.Substring(pp + p.Length).Trim();
                    break;
                }
            }
            if (string.IsNullOrWhiteSpace(soggetto) || string.IsNullOrWhiteSpace(oggetto))
                return "Non ho capito.";

            // rimuoviamo eventuali articoli iniziali
            soggetto = RimuoviArticolo(soggetto);
            oggetto = RimuoviArticolo(oggetto);

            // Comandi di interrogazione inversa
            foreach (string c in InverseQueryCommands)
            {
                if (soggetto.StartsWith(c, StringComparison.CurrentCultureIgnoreCase))
                {
                    List<string> r = InverseQuery(oggetto);
                    if (r.Count > 0)
                    {
                        return r.Aggregate((i, j) => i + "\n" + j);
                    }
                    else
                    {
                        return "Non lo so.";
                    }
                }
            }

            // e inseriamo i dati
            Store(soggetto, oggetto);

            return ret;
        }

        /// <summary>
        /// Rimuove l'articolo iniziale 
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private string RimuoviArticolo(string s)
        {
            string ret = s;
            foreach (string a in Articoli)
            {
                if (s.StartsWith(a, StringComparison.CurrentCultureIgnoreCase))
                {
                    ret = s.Substring(a.Length);
                    break;
                }
            }
            return ret.Trim();
        }

        /// <summary>
        /// Inserisce una informazione nel database
        /// </summary>
        /// <param name="soggetto">soggetto per il quale si carica l'informazione</param>
        /// <param name="oggetto">informazione da inserire</param>
        public void Store(string soggetto, string oggetto)
        {
            List<string> dati;
            soggetto = soggetto.ToUpper();
            if (db.TryGetValue(soggetto, out dati))
            {
                // Ho trovato il soggetto, verifico che non esista anche il soggetto
                if (!dati.Any(o => o.Equals(oggetto, StringComparison.CurrentCultureIgnoreCase)))
                {
                    dati.Add(oggetto);
                }
            }
            else
            {
                // Non ho trovato il soggetto: lo inserisco
                dati = new();
                dati.Add(oggetto);
                db.Add(soggetto, dati);
            }
        }

        /// <summary>
        /// Ritorna tutte le informazioni sul soggetto
        /// </summary>
        /// <param name="soggetto"></param>
        /// <returns></returns>
        public List<string> Query(string soggetto)
        {
            List<string> ret = new();
            List<string> result;
            soggetto = soggetto.ToUpper();
            if (db.TryGetValue(soggetto, out result))
            {
                ret.AddRange(result);
                foreach(string o in result)
                {
                    ret.AddRange(Query(o));
                }
            }
            return ret;
        }

        /// <summary>
        /// Ritorna tutte le informazioni sull'oggetto
        /// </summary>
        /// <param name="soggetto"></param>
        /// <returns></returns>
        public List<string> InverseQuery(string oggetto)
        {
            List<string> ret = new();
            foreach(KeyValuePair<string, List<string>> entry in db)
            {
                if (entry.Value.Any(o => o.Equals(oggetto, StringComparison.CurrentCultureIgnoreCase)))
                {
                    ret.Add(entry.Key);
                    ret.AddRange(InverseQuery(entry.Key));
                }
            }
            return ret;
        }

        /// <summary>
        /// Salva il database su un file
        /// </summary>
        /// <param name="filename"></param>
        public void Save(string filename)
        {
            string ser = JsonSerializer.Serialize(db);
            File.WriteAllText(filename, ser);
        }

        /// <summary>
        /// Salva sul file di default
        /// </summary>
        public void Save() => Save(FileName);

        /// <summary>
        /// Carica il database da un file
        /// </summary>
        /// <param name="filename"></param>
        public void Load(string filename)
        {
            string ser = File.ReadAllText(filename);
            db = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(ser);
        }

        /// <summary>
        /// Carica dal file di default
        /// </summary>
        public void Load() => Load(FileName);
    }
}
