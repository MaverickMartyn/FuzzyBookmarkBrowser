using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace FuzzyBookmarkBrowser
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class Child
    {
        [JsonProperty("date_added")]
        public string DateAdded { get; set; }

        [JsonProperty("guid")]
        public string Guid { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        private string _name;

        [JsonProperty("name")]
        public string Name
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_name))
                {
                    _name = "[N/A]";
                }
                return _name;
            }
            set {  _name = value; }
        }

        [JsonIgnore]
        public string TypeImg { get { return Type == "folder" ? "./folder.png" : "./file.png"; } }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("meta_info")]
        public MetaInfo MetaInfo { get; set; }
        
        [JsonIgnore]
        public int Likeness { get; set; }

        [JsonProperty("children")]
        public List<Child> Children { get; set; }

        public override string ToString()
        {
            return $"{ Name } ({ Url }) - { Type } - { MetaInfo?.LastVisited }";
        }

        public System.Windows.Media.Brush SearchBg
        {
            get
            {
                return Likeness < 1 ? System.Windows.Media.Brushes.LightSlateGray : System.Windows.Media.Brushes.Transparent;
            }
        }

        [JsonIgnore]
        public bool Hidden { get; set; }
    }

    public class BookmarkBar
    {
        [JsonProperty("children")]
        public List<Child> Children { get; set; }

        [JsonProperty("date_added")]
        public string DateAdded { get; set; }

        [JsonProperty("date_modified")]
        public string DateModified { get; set; }

        [JsonProperty("guid")]
        public string Guid { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }

    public class MetaInfo
    {
        [JsonProperty("last_visited_desktop")]
        public string LastVisitedDesktop { get; set; }

        [JsonProperty("last_visited")]
        public string LastVisited { get; set; }
    }

    public class Other
    {
        [JsonProperty("children")]
        public List<Child> Children { get; set; }

        [JsonProperty("date_added")]
        public string DateAdded { get; set; }

        [JsonProperty("date_modified")]
        public string DateModified { get; set; }

        [JsonProperty("guid")]
        public string Guid { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }

    public class Synced
    {
        [JsonProperty("children")]
        public List<Child> Children { get; set; }

        [JsonProperty("date_added")]
        public string DateAdded { get; set; }

        [JsonProperty("date_modified")]
        public string DateModified { get; set; }

        [JsonProperty("guid")]
        public string Guid { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }

    public class Roots
    {
        [JsonProperty("bookmark_bar")]
        public BookmarkBar BookmarkBar { get; set; }

        [JsonProperty("other")]
        public Other Other { get; set; }

        [JsonProperty("synced")]
        public Synced Synced { get; set; }
    }

    public class Root
    {
        [JsonProperty("checksum")]
        public string Checksum { get; set; }

        [JsonProperty("roots")]
        public Roots Roots { get; set; }

        [JsonProperty("sync_metadata")]
        public string SyncMetadata { get; set; }

        [JsonProperty("version")]
        public int Version { get; set; }
    }


}