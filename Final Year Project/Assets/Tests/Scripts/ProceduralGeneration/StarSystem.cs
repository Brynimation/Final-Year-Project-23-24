using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class StarSystem
{
    public bool starExists;
    public int starRadius;
    public Color starColour;
    public static int totalStars;
    public int starId;
    public string starName;
    private Color[] colours = new Color[6] { Color.white, Color.blue, Color.red, Color.blue, Color.green, Color.yellow };
    private uint nLehmerSeed = 0;

    public StarSystem(int xPos, int yPos)
    {
        nLehmerSeed = (uint)((xPos & 0xffff) << 16 | (yPos & 0xffff));
        //nLehmerSeed =  (uint) (nLehmerSeed | ((xRot & 0xffff) | (yRot & 0xffff)));
        starExists = (randInt(1, 20) == 1) ? true : false;
        if (!starExists) return;
        starId = totalStars;
        totalStars++;
        starRadius = randInt(1, 4);
        starColour = colours[randInt(0, 5)];
    }

    public StarSystem(int xPos, int yPos, int zPos)
    {
        nLehmerSeed = (uint)((xPos & 0xffff) << 16 | (yPos & 0xffff));
        nLehmerSeed = (uint)(nLehmerSeed | zPos & 0xffff);
        //nLehmerSeed =  (uint) (nLehmerSeed | ((xRot & 0xffff) | (yRot & 0xffff)));
        starExists = (randInt(1, 20) == 1) ? true : false;
        if (!starExists) return;
        starId = totalStars;
        totalStars++;
        starRadius = randInt(1, 4);
        starColour = colours[randInt(0, 5)];
    }
    public StarSystem(Vector3 screenCentre) 
    {
        nLehmerSeed = (uint)(((uint)screenCentre.x & 0xffff) << 16 | ((int)screenCentre.y & 0xffff));
        //nLehmerSeed =  (uint) (nLehmerSeed | ((xRot & 0xffff) | (yRot & 0xffff)));
        starExists = (randInt(1, 20) == 1) ? true : false;
        if (!starExists) return;
        starId = totalStars;
        totalStars++;
        starRadius = randInt(1, 4);
        starColour = colours[randInt(0, 5)];
    }
    public StarSystem(float x, float y) 
    {
        int xPos = (int)(x * 10);
        int yPos = (int)(y * 10);
        nLehmerSeed = (uint)((xPos & 0xffff) << 16 | (yPos & 0xffff));
        //nLehmerSeed =  (uint) (nLehmerSeed | ((xRot & 0xffff) | (yRot & 0xffff)));
        starExists = (randInt(1, 20) == 1) ? true : false;
        if (!starExists) return;
        starId = totalStars;
        totalStars++;
        starRadius = randInt(1, 4);
        starColour = colours[randInt(0, 5)];
    }

    /*https://en.wikipedia.org/wiki/Lehmer_random_number_generator
     */
    private uint Lehmer32() 
    {
        nLehmerSeed += 0xe120fc15;
        long tmp = (long)nLehmerSeed * 0x4a39b70d;
        long m1 = (tmp >> 32) ^ tmp;
        tmp = m1 * 0x12fad5c9;
        long m2 = (tmp >> 32) ^ tmp;
        return (uint)m2;
    }
    private int randInt(int min, int max) 
    {
        return (int)(Lehmer32() % (max - min)) + min;
    }
    private double randDouble(double min, double max) 
    {
        return ((double)Lehmer32() / (double)(0x7ffffff)) * (max - min) + min;
    }



}
