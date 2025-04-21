using UnityEngine;
using System;

public static class SpringUtils
{
    /// <summary>
    /// Cached set of motion parameters that can be used to efficiently update
    /// multiple springs using the same time step, angular frequency and damping ratio.
    /// </summary>
    public class DampedSpringMotionParams
    {
        // newPos = posPosCoef*oldPos + posVelCoef*oldVel
        public float posPosCoef, posVelCoef;
        // newVel = velPosCoef*oldPos + velVelCoef*oldVel
        public float velPosCoef, velVelCoef;
    };

    /// <summary>
    /// This function will compute the parameters needed to simulate a damped spring
    /// over a given period of time.
    /// - An angular frequency is given to control how fast the spring oscillates.
    /// - A damping ratio is given to control how fast the motion decays.
    ///     damping ratio > 1: over damped
    ///     damping ratio = 1: critically damped
    ///     damping ratio < 1: under damped
    /// </summary>
    public static void CalcDampedSpringMotionParams(DampedSpringMotionParams pOutParams, float deltaTime, float angularFrequency, float dampingRatio)
    {
        const float epsilon = 0.0001f;

        // force values into legal range
        if (dampingRatio < 0.0f) dampingRatio = 0.0f;
        if (angularFrequency < 0.0f) angularFrequency = 0.0f;

        // if there is no angular frequency, the spring will not move and we can
        // return identity
        if (angularFrequency < epsilon)
        {
            pOutParams.posPosCoef = 1.0f; pOutParams.posVelCoef = 0.0f;
            pOutParams.velPosCoef = 0.0f; pOutParams.velVelCoef = 1.0f;
            return;
        }

        if (dampingRatio > 1.0f + epsilon)
        {
            // over-damped
            float za = -angularFrequency * dampingRatio;
            float zb = angularFrequency * Mathf.Sqrt(dampingRatio * dampingRatio - 1.0f);
            float z1 = za - zb;
            float z2 = za + zb;

            float e1 = Mathf.Exp(z1 * deltaTime);
            float e2 = Mathf.Exp(z2 * deltaTime);

            float invTwoZb = 1.0f / (2.0f * zb); // = 1 / (z2 - z1)

            float e1_Over_TwoZb = e1 * invTwoZb;
            float e2_Over_TwoZb = e2 * invTwoZb;

            float z1e1_Over_TwoZb = z1 * e1_Over_TwoZb;
            float z2e2_Over_TwoZb = z2 * e2_Over_TwoZb;

            pOutParams.posPosCoef = e1_Over_TwoZb * z2 - z2e2_Over_TwoZb + e2;
            pOutParams.posVelCoef = -e1_Over_TwoZb + e2_Over_TwoZb;

            pOutParams.velPosCoef = (z1e1_Over_TwoZb - z2e2_Over_TwoZb + e2) * z2;
            pOutParams.velVelCoef = -z1e1_Over_TwoZb + z2e2_Over_TwoZb;
        }
        else if (dampingRatio < 1.0f - epsilon)
        {
            // under-damped
            float omegaZeta = angularFrequency * dampingRatio;
            float alpha = angularFrequency * Mathf.Sqrt(1.0f - dampingRatio * dampingRatio);

            float expTerm = Mathf.Exp(-omegaZeta * deltaTime);
            float cosTerm = Mathf.Cos(alpha * deltaTime);
            float sinTerm = Mathf.Sin(alpha * deltaTime);

            float invAlpha = 1.0f / alpha;

            float expSin = expTerm * sinTerm;
            float expCos = expTerm * cosTerm;
            float expOmegaZetaSin_Over_Alpha = expTerm * omegaZeta * sinTerm * invAlpha;

            pOutParams.posPosCoef = expCos + expOmegaZetaSin_Over_Alpha;
            pOutParams.posVelCoef = expSin * invAlpha;

            pOutParams.velPosCoef = -expSin * alpha - omegaZeta * expOmegaZetaSin_Over_Alpha;
            pOutParams.velVelCoef = expCos - expOmegaZetaSin_Over_Alpha;
        }
        else
        {
            // critically damped
            float expTerm = Mathf.Exp(-angularFrequency * deltaTime);
            float timeExp = deltaTime * expTerm;
            float timeExpFreq = timeExp * angularFrequency;

            pOutParams.posPosCoef = timeExpFreq + expTerm;
            pOutParams.posVelCoef = timeExp;

            pOutParams.velPosCoef = -angularFrequency * timeExpFreq;
            pOutParams.velVelCoef = -timeExpFreq + expTerm;
        }
    }

    /// <summary>
    /// This function will update the supplied position and velocity values over
    /// according to the motion parameters.
    /// </summary>
    public static void UpdateDampedSpringMotion(ref float pPos, ref float pVel, float equilibriumPos, DampedSpringMotionParams springParams)
    {
        float oldPos = pPos - equilibriumPos; // update in equilibrium relative space
        float oldVel = pVel;

        pPos = oldPos * springParams.posPosCoef + oldVel * springParams.posVelCoef + equilibriumPos;
        pVel = oldPos * springParams.velPosCoef + oldVel * springParams.velVelCoef;
    }
}



