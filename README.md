# PowerMesExtExamples
Esempi di codice per creare extension PowerMES

## Extensions PowerMES
* Progetto di tipo class library
* Estensione DLL: «.MesExt.dll»
* Una sola extension per DLL
* Referenziare le librerie: 
*  Atys.PowerMES.Contracts
    *  Atys.PowerMES.Contracts
    * Atys.PowerMES.Foundation
    * Opzionale Atys.PowerMES.Support
* Deve essere creata una classe pubblica
    * Deve implementare l’interfaccia «IMesExtension»
    * Deve essere decorata con l’attributo «ExtensionData»
    * Aggiungere using: Atys.PowerMES.Extensibility, Atys.PowerMES.Foundation, Atys.PowerMES.Support


La DLL deve essere posizionata in una sotto-cartella del percorso di installazione «C:\Program Files (x86)\Atys\PowerMES\Extensions», una DLL per ogni sotto-cartella

## Esempi
1. 005_Foundations: Premesse di base 
2. 010_Empty: Struttura di base per la creazione di un extension
3. 015_Events: Utilizzo degli eventi di sistema
4. 020_Store: Memorizzazione di informazioni
5. 025_ExternalCommands: utilizzo dei comandi esterni
6. 030_ScheduledActivity: Pianificazione di un'attività schedulata
7. 035_Attributes: utilizzo degli attributi per configurare l'extension
8. 040_CancelEvent: utilizzo degli eventi cancellabili
9. 045_FileSystemWatcher: parsing di un file di una cartella monitorata
10. 050_DbMonitor: utilizzo un db esterno per integrare informazioni
11. 052_PowerDeviceInteract: interagire con PowerDevice
12. 055_MesInteract: interagire con MES
13. 060_NicimIteract: interagire con Nicim