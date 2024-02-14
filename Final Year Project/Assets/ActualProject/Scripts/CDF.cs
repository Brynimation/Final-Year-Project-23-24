using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public class CDF
{
    float maxRadius;
    float minRadius;
    float bulgeRadius;
    int numIntervals;
    int tableSize;
    float differenceThreshold;
    float centralIntensity;
    float kappa;

    /* The scale length is a characteristic distance that indicates how concentrated the light is towards the center. 
     * It is the radius at which the surface brightness falls to 1/e (about 37%) of its central value I(0). In other words, 
     * if you move away from the center of the galaxy by a distance of a the brightness at that point will be approximately 37% 
     * of the brightness at the center.
      Good params: I0 = 0.4, maxHaloRadius = 4, kappa = 0.01 (produces nice galaxies)
     */
    float scaleLength;

    public CDF(float minRadius, float maxRadius, float bulgeRadius, int numIntervals, int tableSize, float centralIntensity, float kappa, float differenceThreshold, float scaleLength)
    {
        this.minRadius = minRadius;
        this.maxRadius = maxRadius;
        this.bulgeRadius = bulgeRadius;
        this.numIntervals = numIntervals;
        this.tableSize = tableSize;
        this.centralIntensity = centralIntensity;
        this.kappa = kappa;
        this.differenceThreshold = differenceThreshold;
        this.scaleLength = scaleLength;
    }

    float SurfaceBrightnessDistributionPDF(float radius, float centralIntensity, float kappa, float scaleLength, float bulgeRadius)
    {
        return (radius > bulgeRadius) ? SurfaceBrightnessDistributionDisk(radius - bulgeRadius, SurfaceBrightnessDistribtionBulge(bulgeRadius, centralIntensity, kappa), scaleLength) 
            : SurfaceBrightnessDistribtionBulge(radius, centralIntensity, kappa);
    }
    float SurfaceBrightnessDistribtionBulge(float radius, float centralIntensity, float kappa) 
    {
        return centralIntensity * Mathf.Exp(-kappa * Mathf.Pow(radius, 0.25f));
    }
    float SurfaceBrightnessDistributionDisk(float radius, float centralIntensity, float scaleLength) 
    {
        return centralIntensity * Mathf.Exp(-radius / scaleLength);
    }

    // Trapezoidal rule function - used for numerical integration - required to compute the corresponding cdf of our pdf, as the pdf has 
    // no analytical solutions- https://en.wikipedia.org/wiki/Trapezoidal_rule

    float TrapezoidalRule(float minRadius, float maxRadius, int numIntervals, float centralIntensity, float kappa, float scaleLength, float bulgeRadius) 
    {
        float delta = (maxRadius - minRadius) / (float)numIntervals;
        float sum = 0.5f * (SurfaceBrightnessDistributionPDF(minRadius, centralIntensity, kappa, scaleLength, bulgeRadius) + 
            SurfaceBrightnessDistributionPDF(maxRadius, centralIntensity, kappa, scaleLength, bulgeRadius));
        for (int i = 1; i < numIntervals; ++i)
        {
            float curRadius = minRadius + i * delta;
            sum += SurfaceBrightnessDistributionPDF(curRadius, centralIntensity, kappa, scaleLength, bulgeRadius);
        }
        return sum * delta;
    }
    float TrapezoidalRule(float minRadius, float maxRadius, int numIntervals)
    {
        return TrapezoidalRule(minRadius, maxRadius, numIntervals, centralIntensity, kappa, scaleLength, bulgeRadius);
    }
    // Generate the Cumulative Distribution Function for a given radius
    float ComputeCDF(float radius, int numIntervals, float centralIntensity, float kappa, float scaleLength, float bulgeRadius)
    {
        return TrapezoidalRule(0.0f, radius, numIntervals, centralIntensity, kappa, scaleLength, bulgeRadius);
    }
    float ComputeCDF(float radius, int numIntervals)
    {
        return TrapezoidalRule(0.0f, radius, numIntervals);
    }



    /*Binary search - used to find the radius such that the cumulative probability of finding a star within this radius is <= 
    prob, under our surface brightness distribution function.
    */
    float InvertCdf(float prob, float minRadius, float maxRadius, float differenceThreshold, int numIntervals)
    {
        float cdfRadFromProb = 0.0f;

        float curMaxRadius = maxRadius;
        float curMinRadius = minRadius;
        int maxIterations = 200;
        float radFromProb = curMaxRadius; 
        int i = 0;
        if (prob == 0) return 0;
        while ((curMaxRadius - curMinRadius) > differenceThreshold || i < maxIterations)
        {
            cdfRadFromProb = ComputeCDF(radFromProb, numIntervals); // Assume getCDF is a function we've defined to compute the CDF

            if (Mathf.Abs(cdfRadFromProb - prob) < differenceThreshold)
            {
                // We've found a radius that's close enough to the desired CDF value
                return radFromProb;
            }
            else if (cdfRadFromProb > prob)
            {
                // The CDF at m is too high, so we look in the lower half
                curMaxRadius = radFromProb;
            }
            else
            {
                // The CDF at m is too low, so we look in the upper half
                curMinRadius = radFromProb;
            }
            i++;
            radFromProb = (curMinRadius + curMaxRadius) / 2.0f;
        }

        // After the loop ends, m is an approximation of the inverse CDF value
        return radFromProb;
    }

    float InvertCdf(float prob, float minRadius, float maxRadius, float differenceThreshold, int numIntervals, float centralIntensity, float kappa, float scaleLength, float bulgeRadius)
    {
        float cdfRadFromProb = 0.0f;

        float curMaxRadius = maxRadius;
        float curMinRadius = minRadius;
        int maxIterations = 200;
        float radFromProb = curMaxRadius;
        int i = 0;
        if (prob == 0) return 0;
        while ((curMaxRadius - curMinRadius) > differenceThreshold || i < maxIterations)
        {
            cdfRadFromProb = ComputeCDF(radFromProb, numIntervals, centralIntensity, kappa, scaleLength, bulgeRadius); // Assume getCDF is a function we've defined to compute the CDF

            if (Mathf.Abs(cdfRadFromProb - prob) < differenceThreshold)
            {
                // We've found a radius that's close enough to the desired CDF value
                return radFromProb;
            }
            else if (cdfRadFromProb > prob)
            {
                // The CDF at m is too high, so we look in the lower half
                curMaxRadius = radFromProb;
            }
            else
            {
                // The CDF at m is too low, so we look in the upper half
                curMinRadius = radFromProb;
            }
            i++;
            radFromProb = (curMinRadius + curMaxRadius) / 2.0f;
        }

        // After the loop ends, m is an approximation of the inverse CDF value
        return radFromProb;
    }

    public float[] GenerateInverseCDFLookUpArray() 
    {
        //generate numValues of evenly spaced probabilities and store in an array.
        float[] probabilities = Enumerable.Range(0, tableSize).Select(i => 0.0f + i * (1.0f - 0.0f) / (tableSize - 1)).ToArray();
        float[] lookupTable = probabilities.Select(p => InvertCdf(p, 0.0f, 40f, 0.01f, 1000, 1.0f, kappa, 0.002f, 0.5f)).ToArray();
        lookupTable = probabilities.Select(p => InvertCdf(p, minRadius, maxRadius, differenceThreshold, numIntervals)).ToArray();
        Debug.Log($"Testy thing: {InvertCdf(0.99f, 0.0f, 40f, 0.01f, 1000, 1.0f, kappa, 0.002f, 0.5f)}");
        //Debug.Log($"Final prob: {probabilities[probabilities.Length - 1]}");
        foreach (var rad in lookupTable) Debug.Log(rad);
        return lookupTable;
    }
}
