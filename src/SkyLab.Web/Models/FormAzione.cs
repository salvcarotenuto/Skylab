namespace SkyLab.Web.Models;

public static class FormAzione
{
    public const int Nessuna=0,Visualizzazione=1,Inserimento=2,Modifica=3,Cancellazione=4,Zoom=5,Stampa=8,Login=11,Logout=12;
    public const int Modale=100,OrigineFe=200,CodiceBloccato=400;
    public static int Base(int azione)=>Math.Abs(azione)%100;
    public static int Contesto(int azione)=>Math.Abs(azione)/100;
    public static bool HasContesto(int azione,int contesto)=>contesto>0&&contesto%100==0&&(Contesto(azione)&(contesto/100))==contesto/100;
    public static int WithContesto(int azioneBase,params int[] contesti){var mask=0;foreach(var contesto in contesti)if(contesto>0&&contesto%100==0)mask|=contesto/100;return azioneBase+mask*100;}
    public static bool IsVisualizzazione(int azione)=>Base(azione)==Visualizzazione;
    public static bool IsInserimento(int azione)=>Base(azione)==Inserimento;
    public static bool IsModifica(int azione)=>Base(azione)==Modifica;
    public static bool IsCancellazione(int azione)=>Base(azione)==Cancellazione;
    public static bool IsZoom(int azione)=>Base(azione)==Zoom;
    public static bool IsStampa(int azione)=>Base(azione)==Stampa;
    public static bool IsModal(int azione)=>HasContesto(azione,Modale);
    public static bool IsReadonly(int azione)=>IsVisualizzazione(azione)||IsCancellazione(azione)||IsZoom(azione)||IsStampa(azione);
    public static int ForRecord(bool hasRecord)=>hasRecord?Modifica:Inserimento;
    public static int Normalize(int azione,int fallback)=>Base(azione) is Visualizzazione or Inserimento or Modifica or Cancellazione or Zoom or Stampa or Login or Logout?azione:fallback;
}
