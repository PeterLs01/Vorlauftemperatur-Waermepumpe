using System.Configuration;
using System.Collections.Specialized;
using System.Net.Sockets;

namespace Protokoll_Aussentemp_Innentemp_taeglich
{
    internal class Program
    {
        private static String IPadresseWaermepumpe;
        private static String speicherOrt;
        private static int port = 502;
        private static string temperaturInnen;
        private static string temperaturAussen;
        private static DateTime aktDate = DateTime.Now;

        static void Main(string[] args)
        {
            IPadresseWaermepumpe = ConfigurationManager.AppSettings.Get("IPadresseWaermepumpe");
            speicherOrt = ConfigurationManager.AppSettings.Get("Speicherort");
            if (neuerTag())
            {
                temperaturAussen = leseWert(507, 10.0);
                temperaturInnen = leseWert(588, 10.0);
                speichereDaten();
            }
        }

        private static Boolean neuerTag()
        {
            string[] zeilen;
            int zeilenZahl;
            string[] eintraege;
            string aktDatum;
            try
            {
                if (!File.Exists(@speicherOrt))
                {   //es gibt noch keine Datei
                    using (StreamWriter logFile = new StreamWriter(@speicherOrt, true))
                    {
                        logFile.WriteLine("Datum" + "\t" + "Uhrzeit" + "\t" + "Außentemperatur" + "\t" + "Innentemperatur");
                    }
                    return true;
                }

                using (StreamReader sr = new StreamReader(@speicherOrt))
                {   //Datei einlesen
                    zeilen = sr.ReadToEnd().Split("\n");                 
                }
                zeilenZahl = zeilen.Length;
                eintraege = zeilen[zeilenZahl - 2].Split("\t");   //letzte Zeile ist immer leer
                var datum = DateTime.Parse(eintraege[0]).ToString("d");
                if (aktDate.ToString("d").Equals(datum))
                {   //dieses Datum gibt es schon
                    return false;
                }
                else                                                 
                {   //dieses Datum gibt es noch nicht
                    return true;
                }
            }
            catch (Exception ex) {   //Probleme mit dem Pfad
                    //Console.WriteLine("ArgumentException: " + ex.ToString());
                return false; 
            }                       
        }

        private static string leseWert(ushort modBusAdresse, double teiler)
        {
            TcpClient clientClient = null;
            NetworkStream clientStream = null;
            try
            {
                clientClient = new TcpClient(IPadresseWaermepumpe, port);

                //Übertragung im Format Big Endian
                Byte[] daten = { 0x00, 0x01, // 2 Bytes Transaction identifier beliebiger Wert, wird bei der Antwort wiederholt
                                 0x00, 0x00, // 2 Bytes Protokoll-Kennung immer 0x00,0x00
                                 0x00, 0x06, // 2 Bytes Länge der incl Unit ID
                                 0x01,       // 1 Byte Unit ID 
                                 0x04,       // 1 Byte Funktionscode Lesen des analogen Ausgangs
                                 0x00, 0x00, // 2 Byte Adresse des ersten zu lesenden Registers
                                 0x00, 0x01  // Anzahl der zu bearbeitenden Register
                                 };
                modBusAdresse--;  // auszulesende Modbusadresse -1, da 1 basiert (siehe Anleitung)
                daten[8] = (byte)(modBusAdresse / 256);
                daten[9] = (byte)(modBusAdresse % 256);

                clientStream = clientClient.GetStream();
                clientStream.Write(daten, 0, daten.Length);

                // Antwort empfangen
                Byte[] bytes = new Byte[1024];
                int anzGeleseneBytes = clientStream.Read(bytes, 0, bytes.Length);
                Byte[] bytesToConvert = new Byte[2];
                bytesToConvert[0] = bytes[10];
                bytesToConvert[1] = bytes[9];
                double temp = (BitConverter.ToInt16(bytesToConvert)) / teiler;
                return temp.ToString("0.0");
            }
            catch (ArgumentException e1)
            {
                //Console.WriteLine("ArgumentException: " + e1.ToString());
                return " ";
            }
            catch (SocketException e2)
            {
                //Console.WriteLine(e2.ToString());
                return " ";
            }
            finally
            {
                if (clientStream != null) clientStream.Dispose();
                if (clientClient != null) clientClient.Close();
            }
        }
        private static void speichereDaten()
        {
            using (StreamWriter logFile = new StreamWriter(@speicherOrt, true))   //append
            {
                logFile.WriteLine(aktDate.ToString("d") + "\t" + aktDate.ToString("t") + "\t" + temperaturAussen + "\t" + temperaturInnen);
            }
        }
    }
}