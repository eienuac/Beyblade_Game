using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "PartDatabase", menuName = "Beyblade/Part Database")]
public class PartDatabase : ScriptableObject
{
    public List<BeybladePart> allParts = new List<BeybladePart>();

    public BeybladePart GetPartByID(string id)
    {
        foreach (BeybladePart p in allParts)
        {
            if (p.partID == id) return p;
        }
        return null;
    }
}
