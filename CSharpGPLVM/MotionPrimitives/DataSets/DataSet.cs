using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GPLVM.Styles;

namespace MotionPrimitives.DataSets
{
    // A collection of BHV data files names with corresponding style descriptions

    public class StyleDesc
    {
        private string name;
        private List<string> substyles = new List<string>();

        public StyleDesc(string styleName)
        {
            name = styleName;
        }

        public string Name
        { 
            get { return name; } 
        }

        public List<string> Substyles
        {
            get { return substyles; }
        }

        public void AddSubstyle(string substyleName)
        {
            if (!substyles.Contains(substyleName))
                substyles.Add(substyleName);
        }
    }

    public class StyleDescAndValue
    {
        private StyleDesc styleDesc;
        private int substyleID;
        
        public StyleDescAndValue(StyleDesc styleDesc, string substyleValue)
        {
            this.styleDesc = styleDesc;
            substyleID = styleDesc.Substyles.IndexOf(substyleValue);
            if (substyleID == -1)
                throw new Exception("Substyle " + substyleValue + "is not found in style " + styleDesc.Name);
        }

        public int SubstyleID
        {
            get { return substyleID; }
        }

        public string Substyle
        {
            get { return styleDesc.Substyles[substyleID]; }
        }
    }

    public class FileDesc
    {
        private string fileName;
        private List<StyleDescAndValue> substyles = new List<StyleDescAndValue>();

        public FileDesc(string fileName)
        {
            this.fileName = fileName;
        }

        public FileDesc(string fileName, StyleDesc styleDesc, string substyle)
        {
            this.fileName = fileName;
            AddSubstyleValue(styleDesc, substyle);
        }

        public FileDesc(string fileName, 
            StyleDesc styleDesc1, string substyle1, 
            StyleDesc styleDesc2, string substyle2)
        {
            this.fileName = fileName;
            AddSubstyleValue(styleDesc1, substyle1);
            AddSubstyleValue(styleDesc2, substyle2);
        }

        public string FileName
        {
            get { return fileName; }
        }

        public List<StyleDescAndValue> Substyles
        {
            get { return substyles; }
        }

        public void AddSubstyleValue(StyleDesc styleDesc, string substyle)
        {
            substyles.Add(new StyleDescAndValue(styleDesc, substyle));
        }
    }

    public class DataSet
    {
        public List<StyleDesc> Styles = new List<StyleDesc>();
        public List<FileDesc> Files = new List<FileDesc>();
    }

    public static class EmotionalWalks
    {
        public static string PATH = @"..\..\..\..\..\..\Data\Emotional Walks\";

        public static string EMOTION = "Emotion";
        public static string PERSON = "Person";

        [Flags]
        public enum Emotion
        {
            None = 0,
            Neutral = 1,
            Angry = 2,
            Happy = 4,
            Sad = 8,
            Fear = 16,
            All = 1 + 2 + 4 + 8 + 16,
        }

        [Flags]
        public enum Person
        {
            None = 0,
            Danica = 1,
            Hannes = 2,
            Katrin = 4,
            Kerstin = 8,
            Niko = 16,
            All = 1 + 2 + 4 + 8 + 16,
        }

        private class FileInfo
        {
            public Person Person;
            public Emotion Emotion;
            public string FileName;
            public FileInfo(Person person, Emotion emotion, string fileName)
            {
                Person = person;
                Emotion = emotion;
                FileName = fileName;
            }

            public FileInfo(Person person, Emotion emotion, int trial)
            {
                Person = person;
                Emotion = emotion;
                FileName = Person.ToString() + @"\BVH\" + Person.ToString() + "Mapped_" + emotion.ToString() + "Walk" +  trial.ToString("D2") + ".bvh";
            }
        }

        private static FileInfo[] FileInfos = new FileInfo[]
        {
            new FileInfo(Person.Danica, Emotion.Neutral, 1),

            new FileInfo(Person.Danica, Emotion.Angry, 1),
            new FileInfo(Person.Danica, Emotion.Angry, 2),
            new FileInfo(Person.Danica, Emotion.Angry, 3),
            
            new FileInfo(Person.Danica, Emotion.Happy, 1),
            new FileInfo(Person.Danica, Emotion.Happy, 2),

            new FileInfo(Person.Danica, Emotion.Sad, 2),
            new FileInfo(Person.Danica, Emotion.Sad, 3),

            new FileInfo(Person.Hannes, Emotion.Neutral, 1),
            new FileInfo(Person.Hannes, Emotion.Neutral, 2),

            new FileInfo(Person.Hannes, Emotion.Angry, 1),
            new FileInfo(Person.Hannes, Emotion.Angry, 2),
            new FileInfo(Person.Hannes, Emotion.Angry, 3),
            new FileInfo(Person.Hannes, Emotion.Angry, 4),

            new FileInfo(Person.Hannes, Emotion.Fear, 1),
            new FileInfo(Person.Hannes, Emotion.Fear, 3),

            new FileInfo(Person.Hannes, Emotion.Happy, 1),
            new FileInfo(Person.Hannes, Emotion.Happy, 5),

            new FileInfo(Person.Hannes, Emotion.Sad, 1),
            new FileInfo(Person.Hannes, Emotion.Sad, 3),
            new FileInfo(Person.Hannes, Emotion.Sad, 5),
            new FileInfo(Person.Hannes, Emotion.Sad, 6),

            //new FileInfo(Person.Niko, Emotion.Neutral, 1), // bad
            new FileInfo(Person.Niko, Emotion.Neutral, 2), // OK
            //new FileInfo(Person.Niko, Emotion.Neutral, 3), // bad
            //new FileInfo(Person.Niko, Emotion.Neutral, 4), // terrible
            //new FileInfo(Person.Niko, Emotion.Neutral, 5), // bad

            new FileInfo(Person.Niko, Emotion.Angry, 1),
            new FileInfo(Person.Niko, Emotion.Angry, 2),
            new FileInfo(Person.Niko, Emotion.Angry, 3),

            new FileInfo(Person.Niko, Emotion.Fear, 1),
            new FileInfo(Person.Niko, Emotion.Fear, 2),
            new FileInfo(Person.Niko, Emotion.Fear, 3),

            new FileInfo(Person.Niko, Emotion.Happy, 1),
            new FileInfo(Person.Niko, Emotion.Happy, 2),
            new FileInfo(Person.Niko, Emotion.Happy, 3),
            new FileInfo(Person.Niko, Emotion.Happy, 4),

            new FileInfo(Person.Niko, Emotion.Sad, 1),
            new FileInfo(Person.Niko, Emotion.Sad, 2),
            new FileInfo(Person.Niko, Emotion.Sad, 3),

            new FileInfo(Person.Kerstin, Emotion.Neutral, 1),
            new FileInfo(Person.Kerstin, Emotion.Neutral, 2),
            new FileInfo(Person.Kerstin, Emotion.Neutral, 3),
            new FileInfo(Person.Kerstin, Emotion.Neutral, 4),
            new FileInfo(Person.Kerstin, Emotion.Neutral, 5),

            new FileInfo(Person.Kerstin, Emotion.Angry, 1),
            new FileInfo(Person.Kerstin, Emotion.Angry, 2),
            new FileInfo(Person.Kerstin, Emotion.Angry, 3),
            new FileInfo(Person.Kerstin, Emotion.Angry, 3),
            new FileInfo(Person.Kerstin, Emotion.Angry, 5),

            new FileInfo(Person.Kerstin, Emotion.Fear, 2),
            new FileInfo(Person.Kerstin, Emotion.Fear, 3),
            new FileInfo(Person.Kerstin, Emotion.Fear, 5),

            new FileInfo(Person.Kerstin, Emotion.Happy, 1),
            new FileInfo(Person.Kerstin, Emotion.Happy, 2),

            new FileInfo(Person.Kerstin, Emotion.Sad, 2),
        };

        public static DataSet Select(Person persons, Emotion emotions)
        {
            var res = new DataSet();
            var personStyle = new StyleDesc(PERSON);
            res.Styles.Add(personStyle);
            var emotionStyle = new StyleDesc(EMOTION);
            res.Styles.Add(emotionStyle);

            foreach (FileInfo fi in FileInfos)
            {
                if (((persons & fi.Person) == fi.Person) && ((emotions & fi.Emotion) == fi.Emotion))
                {
                    string sPerson = fi.Person.ToString();
                    string sEmotion = fi.Emotion.ToString();
                    emotionStyle.AddSubstyle(sEmotion);
                    personStyle.AddSubstyle(sPerson);
                    res.Files.Add(new FileDesc(PATH + fi.FileName, personStyle, sPerson, emotionStyle, sEmotion));
                }
            }
            return res;
        }

    }
}
