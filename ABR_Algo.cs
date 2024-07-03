using System.Collections.Generic;
using System;
public class ABRAlgorithm
{
    public Representation GetBestRepresentation(List<Representation> representations)
    {
        // Simple ABR algorithm that selects the highest bandwidth representation
        Representation randomRep = null;
        int randST =1;
        var rand = new Random();
        randST = rand.Next(0,6);
        randomRep = representations[randST];
        return randomRep;
        
    }
}
