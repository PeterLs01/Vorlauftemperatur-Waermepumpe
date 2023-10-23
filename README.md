# Programm zur Unterstützung derEinstellung der Heizkurve einer Wärmepumpe
## geeignet für Stiebel Eltron WPE-I 12 HK 230 Premium
### PeterLs
### Version 1.0
# Motivation
Die Heizkurve einer Wärmepumpe regelt die Vorlauftemperatur der Heizkörper in Abhängigkeit von der Außentemperatur. Wenn die Heizkurve optimal eingestellt ist, haben alle Räume unabhängig von der Außentemperatur immer die gewünschte Temperatur. Je niedriger die Vorlauftemperatur ist, desto effizienter arbeitet die Wärmepumpe. Ziel ist also, ohne die Raumtemperatur durch einen lokalen Regler am Heizkörper zu erniedrigen, die Vorlauftemperatur minimal einzustellen.

Die Heizkurve wird durch zwei Parameter Komforttemperatur (die angestrebte Raumtemperatur) und die Steilheit (auch Neigung) festgelegt. 

Im Internet gibt es eine Reihe Anleitungen für die optimale Einstellung einer Heizkurve. In der Regel werden folgende Punkte empfohlen:
* Thermostatventile aufdrehen
  Da die o.g. Wärmepumpe mit einem Wärmepumpen-Manager in eine Raum verbunden ist, sind die Ventile zumindest in diesem Raum ganz aufzudrehen.
* Eine Tabelle mit Messungen anlegen:
  Datum, Uhrzeit, Außentemperatur, Innentemperatur.
  Die Vorlauftemperatur lohnt sich m.E. nicht zu protokollieren.
* Die Heizkurve nach den Messungen anpassen.

Da ein PC in der Regel jeden Tag eingeschaltet wird, soll der PC einmal am Tag die
Werte von der Wärmepumpe abfragen und in eine Datei schreiben. Um zu prüfen ob
eine Nachtabsenkung noch Einsparmöglichkeiten liefert, könnte man das Protokoll von
einem Raspi stündlich erzeugen lassen.
# Auswertung mit einem Tabellenkalkulationsprogramm
Diese Tabelle öffnet man in einem Tabellenkalkulationsprogramm, erzeugt eine Grafik
aus den Daten und ergänzt das Diagramm um eine Trendlinie.
![Bild1](https://github.com/PeterLs01/Vorlauftemperatur-Waermepumpe/assets/71694571/74a84821-db8e-4023-a317-d1950d3f954a)

Die Trendlinie ist aktuell nicht parallel zur Außentemperaturachse. Bei 0°C Außentemperatur sollte die Vorlauftemperatur etwas höher sein, bei ca 10°C ist sie sinnvoll
eingestellt.

![Bild6](https://github.com/PeterLs01/Vorlauftemperatur-Waermepumpe/assets/71694571/486bd18a-93ee-40c5-9ff1-129e88300594)

Man sieht es gibt zusätzliche Einflüsse wie z.B. Sonne, Starkwind, Lüftungsverhalten
u.a., die auch einen Einfluss auf die Raumtemperatur haben. Aber diese Kurve ist bei
den wenigen Messpunkten schon recht gut. Die Vorlauftemperatur bei 0°C ist stimmig.
Bei höheren Temperaturen leicht zu hoch.

Die Heizkurve wird am Gerät direkt oder am Manager eingestellt. An der Wärmepumpe kann man sich die Heizkurve anzeigen lassen und die Parameter iterativ solange
ändern, bis der gewünschte Verlauf angezeigt wird.

Es wäre angenehmer aus zwei Wertepaaren (Außentemperatur1|Vorlauftemperatur1)
und (Außentemperatur2|Vorlauftemperatur2) direkt die Werte für Komforttemperatur
und Steigung berechnen zu können. Leider kenne ich bisher keine geeignete Formel für
diese Wärmepumpe.

Für interessierte Betreiber wäre eine direkte Eingabe der Vorlauftemperatur bei zwei
Werten der Außentemperatur (0°C und z.B. 12°C) an der Wärmepumpe eine deutliche
Vereinfachung der Anpassung.
# Programm
Die Wärmepumpe steht im lokalen Netz. Werte können mit dem Modbus/TCP Protokoll
von der Wärmepumpe abgefragt werden. Das Programm ist in C# geschrieben und hat
keine Benutzeroberfläche.

Es gibt zwei Möglichkeiten dieses Programm zu installieren:
* In Visual Studio das Programm (mit pull) downloaden, anpassen und übersetzten lassen.
* Das File Vorlauftemperatur.7z downloaden, entpacken, installieren und anpassen.

Anzupassen ist die Datei Protokoll_Aussentemp_Innentemp_taeglich.exe.config. Da
diese Datei im XML Format gespeichert ist, kann man sie mit jedem Editor bearbeiten.
Angepasst wird die IP-Adresse der Wärmepumpe und der Speicherort der Protokolldatei.
Die exe Datei muss noch im Autostart eingetragen werden. (Windows 11: Taskmanager,
Autostart von Apps, neuen Task ausführen)

Wenn man die Vorlauftemperatur auch protokollieren möchte, dann ergänze man im Programm:
* die Variable private static string temperaturVorlauf;
* in der Verzweigung: if (neuerTag())
um die Zeile **temperaturVorlauf = leseWert(511, 10.0);** 
vor der Zeile speichereDaten();
* ergänze in der Zeile logFile.WriteLine(... +"\t" **+ temperaturVorlauf**);
# KNX
Wenn die Heizkreise über KNX gesteuert werden, kann die Abfrage der Raumtemperatur
gezielt im kältesten Raum bei geöffneten Ventilen erfolgen. Für Interessierte hier ein
Ausschnitte aus dem Quellcode für diesen Teil eines C# Programms. Der Zugriff auf
KNX erfolgt über ein IP Interface.

Bitte die Lizenz (KNX Tools Software License Agreement) dieses Teils beachten!

    using Knx.Bus.Common.Configuration;
    using Knx.Bus.Common.GroupValues;
    using Knx.Falcon.Sdk;
    //...
    private static Bus_bus = null;
    //...
    static void konnektKNXBus ( )
    {
        _bus = new Bus(new KnxIpTunnelingConnectorParameters("192.168.2.3",0x0e57,false));
        try
        {
            _bus.Connect();
            Thread.Sleep(200);
            temperatur = leseTemperatur("0/0/37");
            _bus.Disconnect();
        }
        catch(Exception e)
        {
            Console.WriteLine("keine Verbindung zu KNX/EIB");
        }
    }
    private static String leseTemperatur(String gruppenAdresse )
    {
        GroupValue antwort_GV = _bus.ReadValue(gruppenAdresse);
        String [] antwort = antwort_GV.ToString().Split(’$’) ;
        antwort[1] = antwort[1].Substring(0,2);
        antwort[2] = antwort[2].Substring(0,2);
        int byte1 = Convert.ToInt32(antwort[1],16);
        double erg = 0;
        if (byte1 > 127)
            erg = −1;
        else
            erg = 1;
        int exponent = (byte1 % 128)/8 ;
        int mantisse = (byte1 % 4) * 256 + Convert.ToInt32(antwort[2],16);
        erg *= Math.Pow(2,exponent) * mantisse / 100;
        return erg.ToString();
    }
