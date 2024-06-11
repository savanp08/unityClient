using System.Collections.Generic;

public class ABRAlgorithm
{
    public Representation GetBestRepresentation(List<Representation> representations)
    {
        // Simple ABR algorithm that selects the highest bandwidth representation
        Representation bestRep = null;
        foreach (var rep in representations)
        {
            if (bestRep == null || rep.Bandwidth > bestRep.Bandwidth)
            {
                bestRep = rep;
            }
        }
        return bestRep;
    }
}
