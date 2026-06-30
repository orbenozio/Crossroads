using UnityEngine;

namespace Crossroads.Engine
{
    public enum BreakOn { Min, Max, Both }

    // הגדרת משאב בודד (ספק 14.2). id הוא המפתח ש-story.json מתייחס אליו.
    [System.Serializable]
    public sealed class ResourceDef
    {
        public string id;
        public string displayName;   // תווית ברירת-מחדל; ה-Theme יכול לדרוס (Theme override > ResourceDef, ספק 14.2)
        public int min = 0;
        public int max = 10;
        public int start = 5;
        public BreakOn breakOn = BreakOn.Both;
        public int dangerBand = 1;   // מרחק-מהקצה שמפעיל מצב-סכנה (ספק 10.4)
    }

    // resources.asset - ScriptableObject. הסדר במערך הוא סדר-המדים הקבוע (ספק 8.2)
    // וגם ברירת-מחדל לקדימות שבירה-כפולה (Q1: הסדר קובע).
    // נוצר ב-Editor דרך Create > Crossroads > Resource Set (לא נכתב ידנית כ-YAML).
    [CreateAssetMenu(fileName = "resources", menuName = "Crossroads/Resource Set", order = 1)]
    public sealed class ResourceSet : ScriptableObject
    {
        public ResourceDef[] resources = new ResourceDef[0];

        public ResourceDef Find(string id)
        {
            for (int i = 0; i < resources.Length; i++)
            {
                if (resources[i].id == id) return resources[i];
            }
            return null;
        }
    }
}
