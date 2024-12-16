using System.Collections.Generic;
using System;
public class ABRAlgorithm
{
    public int representationIndex = 0;
    public Representation GetBestRepresentation(List<Representation> representations)
    {
        // Simple ABR algorithm that selects the highest bandwidth representation
        Representation randomRep = null;
        int randST =1;
        var rand = new Random();
        randST = rand.Next(0,2);
        randomRep = representations[randST];
        return randomRep;
        
    }
}
