using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using UnityEngine.Networking;

public class MPDParser
{
    private List<Representation> representations = new List<Representation>();

    public IEnumerator FetchMPD(string url)
    {
        Debug.Log("----->>>>> Fetching MPD from: " + url);
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            yield return webRequest.SendWebRequest();
            if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("----->>>>> MPD fetch error: " + webRequest.error);
                yield break;
            }
            ParseMPD(webRequest.downloadHandler.text);
        }
    }

    void ParseMPD(string xmlText)
    {
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(xmlText);
        XmlNamespaceManager namespaceManager = new XmlNamespaceManager(xmlDoc.NameTable);
        namespaceManager.AddNamespace("mpd", "urn:mpeg:dash:schema:mpd:2011");

        XmlNodeList xmlRepresentations = xmlDoc.SelectNodes("//mpd:AdaptationSet/mpd:Representation", namespaceManager);
        foreach (XmlNode xmlNode in xmlRepresentations)
        {
            if (xmlNode is XmlElement repElement)
            {
                Representation rep = new Representation
                {
                    Id = repElement.GetAttribute("id"),
                    Bandwidth = int.Parse(repElement.GetAttribute("bandwidth")),
                    Media = ((XmlElement)repElement.ParentNode.SelectSingleNode("mpd:SegmentTemplate", namespaceManager)).GetAttribute("media"),
                    Initialization = ((XmlElement)repElement.ParentNode.SelectSingleNode("mpd:SegmentTemplate", namespaceManager)).GetAttribute("initialization"),
                    Timescale = int.Parse(((XmlElement)repElement.ParentNode.SelectSingleNode("mpd:SegmentTemplate", namespaceManager)).GetAttribute("timescale")),
                    Duration = int.Parse(((XmlElement)repElement.ParentNode.SelectSingleNode("mpd:SegmentTemplate", namespaceManager)).GetAttribute("duration"))
                };
                representations.Add(rep);
            }
        }
    }

    public List<Representation> GetRepresentations()
    {
        return representations;
    }
}

public class Representation
{
    public string Id;
    public int Bandwidth;
    public string Media;
    public string Initialization;
    public int Timescale;
    public int Duration;
}


// using System.Collections.Generic;
// using System.Xml;
// using UnityEngine;

// public class MPDParser
// {
//     public List<Representation> ParseMPD(string xmlText)
//     {
//         List<Representation> representations = new List<Representation>();
//         XmlDocument xmlDoc = new XmlDocument();
//         xmlDoc.LoadXml(xmlText);
//         XmlNamespaceManager namespaceManager = new XmlNamespaceManager(xmlDoc.NameTable);
//         namespaceManager.AddNamespace("mpd", "urn:mpeg:dash:schema:mpd:2011");

//         XmlNodeList xmlRepresentations = xmlDoc.SelectNodes("//mpd:AdaptationSet/mpd:Representation", namespaceManager);
//         foreach (XmlNode xmlNode in xmlRepresentations)
//         {
//             if (xmlNode is XmlElement repElement)
//             {
//                 Representation rep = new Representation
//                 {
//                     Id = repElement.GetAttribute("id"),
//                     Bandwidth = int.Parse(repElement.GetAttribute("bandwidth")),
//                     Media = ((XmlElement)repElement.ParentNode.SelectSingleNode("mpd:SegmentTemplate", namespaceManager)).GetAttribute("media"),
//                     Timescale = int.Parse(((XmlElement)repElement.ParentNode.SelectSingleNode("mpd:SegmentTemplate", namespaceManager)).GetAttribute("timescale"))
//                 };
//                 representations.Add(rep);
//             }
//         }
//         return representations;
//     }
// }
