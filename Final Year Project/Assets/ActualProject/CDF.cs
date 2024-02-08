using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CDF
{
    int maxRadius;
    int minRadius;
    int numIntervals;
    int tableSize;
    float differenceThreshold;
    float centralIntensity;
    float kappa;

    public CDF(int maxRadius, int minRadius, int numIntervals, int tableSize, float centralIntensity, float kappa, float differenceThreshold)
    {
        this.maxRadius = maxRadius;
        this.minRadius = minRadius;
        this.numIntervals = numIntervals;
        this.tableSize = tableSize;
        this.centralIntensity = centralIntensity;
        this.kappa = kappa;
        this.differenceThreshold = differenceThreshold;
    }

    float SurfaceBrightnessDistributionPDF(float radius)
    {
        return centralIntensity * Mathf.Exp(-kappa * Mathf.Pow(radius, 0.25f));
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
        float radFromProb = 0.0f;

        int maxIterations = 2000;
        int i = 0;
        while ((maxRadius - minRadius) > differenceThreshold || i < maxIterations)
        {
            radFromProb = (minRadius + maxRadius) / 2.0f;
            cdfRadFromProb = ComputeCDF(radFromProb, numIntervals); // Assume getCDF is a function we've defined to compute the CDF

            if (Mathf.Abs(cdfRadFromProb - prob) < differenceThreshold)
            {
                // We've found a radius that's close enough to the desired CDF value
                return radFromProb;
            }
            else if (cdfRadFromProb > prob)
            {
                // The CDF at m is too high, so we look in the lower half
                maxRadius = radFromProb;
            }
            else
            {
                // The CDF at m is too low, so we look in the upper half
                minRadius = radFromProb;
            }
            i++;
        }

        // After the loop ends, m is an approximation of the inverse CDF value
        return radFromProb;
    }

    float[] GenerateInverseCDFLookUpArray() 
    {
        //generate numValues of evenly spaced probabilities and store in an array.
        float[] probabilities = Enumerable.Range(0, tableSize).Select(i => 0.0f + i * (1.0f - 0.0f) / (tableSize - 1)).ToArray();
        float[] lookupTable = probabilities.Select(p => InvertCdf(p, minRadius, maxRadius, differenceThreshold, numIntervals)).ToArray();
        return lookupTable;
    }
}
