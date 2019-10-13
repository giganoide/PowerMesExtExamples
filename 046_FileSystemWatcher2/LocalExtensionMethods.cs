using System;
using System.Text;

namespace TeamSystem.Customizations
{
    internal static class LocalExtensionMethods
    {
        /// <summary>
        /// Restituisce la stringa rappresentata da una serie di codici ascii,
        /// oppure null se ci sono eccezioni od errori
        /// </summary>
        /// <param name="inStr">Stringa da elaborare</param>
        /// <returns>La stringa con la sequenza di codici ascii, oppure <c>null</c>
        /// se stringa in ingresso non valida</returns>
        /// <remarks>Mi aspetto codici numerici separati da spazi, compresi 
        /// tra 0 e 255, se oltre 255 oppure dati non validi non vengono
        /// sollevate eccezioni, ma viene restituito <c>null</c>.</remarks>
        public static string GetSafeTextFromAsciiCodes(this string inStr)
        {
            if (inStr.IsNullOrWhiteSpace())
                return string.Empty;

            bool hasErrors = false; //per rilevazione errori conversione
            StringBuilder sb = new StringBuilder(); //più efficiente se stringa risultato nulla

            //ricavo tutti i singoli codici carattere
            string[] codes = inStr.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string code in codes)
            {
                int numCode = 0;
                if (!Int32.TryParse(code, out numCode))
                {
                    //errore di cast, interrompo
                    hasErrors = true;
                    break;
                }

                if (numCode.IsBetweenWithBounds(0, 255))
                {
                    //codice carattere valido
                    sb.Append((char)numCode);
                }
                else
                {
                    //codice carattere non valido, interrompo
                    hasErrors = true;
                    break;
                }
            }
            //se ho errori restituisco null
            return (sb.Length > 0 && !hasErrors) ? sb.ToString() : null;
        }

        /// <summary>
        /// Restituisce se la stringa è nulla o vuota o solo formata da spazi
        /// </summary>
        /// <param name="s">Stringa da elaborare</param>
        /// <returns><c>true</c> se la stringa è vuota o nulla, <c>false</c> altrimenti</returns>
        public static bool IsNullOrWhiteSpace(this string s)
        {
            return string.IsNullOrWhiteSpace(s);
        }
        /// <summary>
        /// Verifica se un numero è compreso tra altri due (oppure coincidente con estremi)
        /// </summary>
        /// <param name="toCompare">Numero da verificare</param>
        /// <param name="first">Estremo inferiore pre il confronto</param>
        /// <param name="second">Estremo superiore pre il confronto</param>
        /// <returns><c>true</c> se il numero da verificare è compreso nell'intervallo,
        /// oppure uguale ad un estremo</returns>
        /// <exception cref="ArgumentException">Se <paramref name="first"/> non è
        /// minore o uguale a <paramref name="second"/></exception>
        public static bool IsBetweenWithBounds(this int toCompare, int first, int second)
        {
            if (first > second)
                throw new ArgumentException();

            return (toCompare >= first) && (toCompare <= second);
        }

    }
}