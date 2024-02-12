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
     * of the brightness at the center.*/
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

    float SurfaceBrightnessDistributionPDF(float radius)
    {
        return (radius > bulgeRadius) ? SurfaceBrightnessDistributionDisk(radius - bulgeRadius) : SurfaceBrightnessDistribtionBulge(radius);
    }
    float SurfaceBrightnessDistribtionBulge(float radius) 
    {
        return centralIntensity * Mathf.Exp(-kappa * Mathf.Pow(radius, 0.25f));
    }
    float SurfaceBrightnessDistributionDisk(float radius) 
    {
        return centralIntensity * Mathf.Exp(radius / scaleLength);
    }

    // Trapezoidal rule function - used for numerical integration - required to compute the corresponding cdf of our pdf, as the pdf has 
    // no analytical solutions- https://en.wikipedia.org/wiki/Trapezoidal_rule
    float TrapezoidalRule(float minRadius, float maxRadius, int numIntervals)
    {
        float delta = (maxRadius - minRadius) / (float)numIntervals;
        float sum = 0.5f * (SurfaceBrightnessDistributionPDF(minRadius) + SurfaceBrightnessDistributionPDF(maxRadius));
        for (int i = 1; i < numIntervals; ++i)
        {
            float curRadius = minRadius + i * delta;
            sum += SurfaceBrightnessDistributionPDF(curRadius);
        }
        return sum * delta;

    }
    // Generate the Cumulative Distribution Function for a given radius
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
                return radFromProb/maxRadius;
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
        return radFromProb/maxRadius;
    }

    public float[] GenerateInverseCDFLookUpArray() 
    {
        //generate numValues of evenly spaced probabilities and store in an array.
        float[] probabilities = Enumerable.Range(0, tableSize).Select(i => 0.0f + i * (1.0f - 0.0f) / (tableSize - 1)).ToArray();
        float[] lookupTable = probabilities.Select(p => InvertCdf(p, minRadius, maxRadius, differenceThreshold, numIntervals)).ToArray();
        Debug.Log($"Final prob: {probabilities[probabilities.Length - 1]}");
        foreach (var rad in lookupTable) Debug.Log(rad);
        return lookupTable;
    }
}
