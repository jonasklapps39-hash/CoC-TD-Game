using UnityEditor;

namespace MuhDefense.EditorTools
{
    /// <summary>Menüpunkt "Muh Defense > Spielstand löschen": setzt den Hof zum Testen auf den Anfang zurück.</summary>
    public static class SaveGameMenu
    {
        [MenuItem("Muh Defense/Spielstand löschen")]
        public static void DeleteSave()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Muh Defense", "Bitte zuerst den Play-Modus beenden.", "OK");
                return;
            }
            if (EditorUtility.DisplayDialog("Muh Defense", "Den gespeicherten Hof wirklich löschen? Beim nächsten Start beginnt ein neuer Hof.", "Löschen", "Abbrechen"))
            {
                FarmState.DeleteSave();
            }
        }
    }
}
