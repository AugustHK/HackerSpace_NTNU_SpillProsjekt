﻿using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;


[System.Serializable]
public struct Highscore
{
    public int score;
    public string name;

    public Highscore(int score, string name)
    {
        this.score = score;
        this.name = name;
    }
}

class HighscoreComparator : IComparer<Highscore>
{
    public int Compare(Highscore x, Highscore y)
    {
        if (x.score == 0)
            return y.score;
        if (y.score == 0)
            return x.score;

        return y.score.CompareTo(x.score);
    }
}

public static class LeaderboardData
{
    // Path to where highscore data is saved
    private static string path = Application.persistentDataPath + "/highscores.data";

    /// <summary>
    /// Add a new score to the leaderboard
    /// </summary>
    public static void AddScore(int score, string name)
    {
        List<Highscore> highscores = LoadScores();
        highscores.Add(new Highscore(score, name));

        // Sort the struct using a custom comparator
        highscores.Sort(new HighscoreComparator());

        SaveScores(highscores);
    }

    /// <summary>
    /// Save all the given scores
    /// </summary>
    public static void SaveScores(List<Highscore> scores)
    {
        BinaryFormatter formatter = new BinaryFormatter();

        FileStream stream = new FileStream(path, FileMode.Create);

        formatter.Serialize(stream, scores);

        stream.Close();
    }

    /// <summary>
    /// Load all saved scores
    /// </summary>
    /// <returns>An ordered list of <c>Highscore</c> structs.</returns>
    public static List<Highscore> LoadScores()
    {
        if (File.Exists(path))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(path, FileMode.Open);

            List<Highscore> data;

            // Check if the file is corrupted
            try
            {
                data = formatter.Deserialize(stream) as List<Highscore>;
            } catch (SerializationException e)
            {
                File.Delete(path);
                Debug.LogWarning($"Highscore data was corrupted, it has been replaced.\nException message: {e.Message}");
                return LoadScores();
            } finally
            {
                stream.Close();
            }

            return data;
        } else
        {
            // No leaderboard exists create a default one
            List<Highscore> highscores = new List<Highscore>
            {
                new Highscore(5000, "The Archetype"),
                new Highscore(4500, "Fuereoduriko"),
                new Highscore(4000, "Dabble"),
                new Highscore(3500, "Frisk"),
                new Highscore(3000, "Jesper"),
                new Highscore(2500, "Rodrigues"),
                new Highscore(2000, "Zedd"),
                new Highscore(1500, "Grønnmerke"),
                new Highscore(1000, "KHTangent"),
                new Highscore(10, "Endie"),
            };

            SaveScores(highscores);
            return highscores;
        }
    }
}
