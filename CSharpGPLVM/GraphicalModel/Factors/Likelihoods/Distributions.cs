using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ILNumerics;

namespace GraphicalModel.Factors.Likelihoods
{
 
    public static class Helper
    {
        public static ILRetArray<double> logFactorial(ILInArray<double> inX)
        {
            using (ILScope.Enter(inX))
            {
                ILArray<double> x = ILMath.check(inX);
                ILArray<double> logFact = 0;
                for (int i = 1; i <= x; i++)
                    logFact = logFact + ILMath.log(Convert.ToDouble(i));
                return logFact;
            }
        }

        public static ILRetArray<double> sumLogFactorial(ILInArray<double> inEventOccurances)
        {
            using (ILScope.Enter(inEventOccurances))
            {
                ILArray<double> eventOccurances = ILMath.check(inEventOccurances);
                ILArray<double> sumLF = 0;
                foreach (int value in eventOccurances)
                    sumLF += Helper.logFactorial(value);
                return sumLF;
            }
        }

        public static ILRetArray<double> logGammaFunction(ILInArray<double> x)
        {
            return logFactorial(x - 1);
        }

        public static ILRetArray<double> sumLogGammaFunction(ILInArray<double> eventOccurances)
        {
            return sumLogFactorial((eventOccurances - ILMath.ones(1,eventOccurances.Length)));
        }

        public static ILRetArray<double> logGammaHalfFunction(ILInArray<double> inX)
        {
            //x = n/2
            using(ILScope.Enter(inX))
            {
                ILArray<double> x = ILMath.check(inX);
                if(ILMath.mod(x,2) == 0) 
                    return logGammaFunction(x); 
                return (0.5 * ILMath.log(ILMath.pi) + logDoubleFactorial(2 * (x - 1)) - (x - 0.5) * ILMath.log(2.0));   

            }
        }

        public static ILRetArray<double> sumLogGammaHalfFunction(ILInArray<double> inRange)
        {
            using(ILScope.Enter(inRange))
            {
                ILArray<double> range = ILMath.check(inRange);
                ILArray<double> val = 0;
                for( int i = 0; i < range.Length; i++)
                {
                    val = val + logGammaHalfFunction(inRange[i]);
                }

                return val;
            }
        }

        public static ILRetArray<double> logMultivariateGammaFunction(ILInArray<double> inX, int L)
        {
            using(ILScope.Enter(inX))
            {
                ILArray<double> x = ILMath.check(inX);
                ILArray<double> val = 0;

                val = (L * (L - 1) / 4 ) * ILMath.log(ILMath.pi) 
                                + Helper.sumLogGammaFunction(x + (ILMath.ones(L) - Convert.ToDouble(Enumerable.Range(1, L)) / 2));
                
                return val;
            }
        }

        public static ILRetArray<double> logDoubleFactorial(ILInArray<double> n)
        {
            if (ILMath.mod(n,2) == 0)
                throw new Exception("logDoubleFactorial: Invalid arguments. Expected an odd number");

            ILArray<double> val = 0;
            for(int i = 1; i < (n + 1) / 2; i++)
            {
                val = val + ILMath.log(Convert.ToDouble(2 * i -1));
            }

            return val;
        }

        public static ILRetArray<double> logDeterminant(ILInArray<double> inX)
        {
            using (ILScope.Enter(inX))
            {
                ILArray<double> x = ILMath.check(inX);
                return (2 * ILMath.sumall(ILMath.log(ILMath.diag(ILMath.chol(x)))));
            }
        }

        public static ILRetArray<double> logBetaFunction(ILInArray<double> inAlpha, ILInArray<double> inBeta)
        {
            using (ILScope.Enter(inAlpha, inBeta))
            {
                ILArray<double> alpha = ILMath.check(inAlpha);
                ILArray<double> beta = ILMath.check(inBeta, (betaIn) =>
                                                     {
                                                         if (betaIn.Length == 1)
                                                             return betaIn;
                                                         else if (betaIn.Length == alpha.Length)
                                                             return betaIn;
                                                         else
                                                             throw new Exception("BetaFunction: Length Mismatch. Refer Matlab documentation");
                                                     });
                ILArray<double> L = 0;
                if (alpha.Length >= beta.Length)
                {
                    for (int i = 0; i < alpha.Length; i++)
                    {
                        L[i] = logGammaFunction(alpha[i]) + logGammaFunction(beta) - logGammaFunction(alpha[i] + beta);
                    }
                }
                else
                {
                    for (int i = 0; i < beta.Length; i++)
                    {
                        L[i] = logGammaFunction(alpha) + logGammaFunction(beta[i]) - logGammaFunction(alpha + beta[i]);
                    }
                }
                return L;
            }

        }
    }

    public static class Multinomial
    {

        public static ILRetArray<double> LogLikelihood(ILInArray<int> inSupport, ILInArray<double> inEventProbabilities, ILInArray<int> inObservations)
        {
            using (ILScope.Enter(inSupport, inEventProbabilities, inObservations))
            {
                ILArray<int> support = ILMath.check(inSupport);
                ILArray<double> eventProbabilities = ILMath.check(inEventProbabilities);
                ILArray<int> observations = ILMath.check(inObservations);
                ILArray<double> L = 0;
                var numberOfEvents = support.Length;
                var numberOfEventOccurances = observations.Length;
                ILArray<double> eventOccurances = ILMath.zeros(numberOfEvents);
                foreach (int value in observations)
                    eventOccurances[ILMath.find(support == value)] += 1 ;
                L = (Helper.logFactorial(numberOfEventOccurances) - Helper.sumLogFactorial(eventOccurances))
                    + ILMath.sumall(eventOccurances * ILMath.log(eventProbabilities));
                eventOccurances = eventOccurances / numberOfEventOccurances;
                return L;
            }

        }

        public static ILRetArray<int> Sample(ILInArray<int> inSupport, ILInArray<double> inEventProbabilities, int numberOfSamples)
        {
           using (ILScope.Enter(inSupport, inEventProbabilities)){
                ILArray<int> support = ILMath.check(inSupport);
                ILArray<double> eventProbabilities = ILMath.check(inEventProbabilities);
                ILArray<int> samples = ILMath.zeros<int>(numberOfSamples);
                for (int i = 0; i < numberOfSamples; i++)
                {
                    ILArray<double> uniRand = ILMath.rand(1);
                    ILArray<double> cumsum = eventProbabilities[0];
                    int index = 1;
                    while (index < eventProbabilities.Length)
                    {
                        if (uniRand < cumsum)
                        {
                            samples[i] = support[index - 1];
                            break;
                        }
                        cumsum = cumsum + eventProbabilities[index];
                        index++;
                    }
                    samples[i] = support[index - 1];
                }
                return samples;
           }
        }

        public static ILRetArray<double> DerivativeLogLikelihoodwrtParams(ILInArray<int> inSupport, ILInArray<double> inEventProbabilities, ILInArray<int> inObservations)
        {
            using (ILScope.Enter(inSupport, inEventProbabilities, inObservations))
            {
                ILArray<int> support = ILMath.check(inSupport);
                ILArray<double> eventProbabilities = ILMath.check(inEventProbabilities);
                ILArray<int> observations = ILMath.check(inObservations);
                ILArray<double> derivative = ILMath.zeros(support.Length);
                ILArray<double> count = 0;

                for (int i = 0; i < support.Length; i++)
                {
                    count = observations.Count((x) => { return x == support[i]; });
                    derivative[i] = (count) / eventProbabilities[i];
                }

                return derivative;
            }
        }
    }

    public static class Gaussian
    {
        public static ILRetArray<double> LogLikelihood(ILInArray<double> inMu, ILInArray<double> inSigma, ILInArray<double> inX)
        {
            using(ILScope.Enter(inMu, inSigma, inX))
            {
                ILArray<double> mu = ILMath.check(inMu);
                ILArray<double> sigma = ILMath.check(inSigma);
                ILArray<double> X = ILMath.check(inX);
                ILArray<double> L = 0;
                var nD = ILMath.size(X, 1);
                var nN = ILMath.size(X, 0);
                //Observations along rows and dimensions along coulmns
                X = X - ILMath.repmat(mu.T, nN);
                L = - ILMath.trace(ILMath.multiply(X, ILMath.pinv(sigma), X.T)) / 2 -
                    ((ILMath.log(2 * ILMath.pi) * nD / 2) + ((nN / 2 ) * Helper.logDeterminant(sigma)));
                return L;
            }

        }

        public static ILRetArray<double> Sample(ILInArray<double> inMu, ILInArray<double> inSigma, int numberOfSamples)
        {
            using(ILScope.Enter(inMu, inSigma))
            {
                return ILMath.mvnrnd(inMu, inSigma, numberOfSamples);
            }
        }

        public static void DerivativeLogLikelihoodwrtParams(ILInArray<double> inMu, ILInArray<double> inSigma, ILInArray<double> inX, ILOutArray<double> dL_dMu = null, ILOutArray<double> dL_dSigma = null)
        {
            using (ILScope.Enter(inMu, inSigma, inX))
            {
                ILArray<double> mu = ILMath.check(inMu);
                ILArray<double> sigma = ILMath.check(inSigma);
                ILArray<double> X = ILMath.check(inX);
                ILArray<double> derivative = ILMath.zeros(2,2);

                var nD = ILMath.size(X, 1);
                var nN = ILMath.size(X, 0);
                X = X - ILMath.repmat(mu.T, nN);

                ILArray<double> invSigma = ILMath.pinv(sigma);
                // Derivative of L wrt mu
                dL_dMu.a = ILMath.sum(ILMath.multiply(invSigma, X.T), 1).T ; 

                
                // Derivative of L wrt sigma
                dL_dSigma.a = -0.5 * (invSigma - ILMath.multiply(invSigma, X.T, X, invSigma));
                //return derivative;
            }

        }

        public static ILRetArray<double> ComputeMean(ILInArray<double> inX)
        {
            using (ILScope.Enter(inX))
            {
                ILArray<double> X = ILMath.check(inX);
                return ILMath.mean(X, 0);
            }
        }

        public static ILRetArray<double> ComputeCovariance(ILInArray<double> inX)
        {
            using (ILScope.Enter(inX))
            {
                ILArray<double> X = ILMath.check(inX);
                return ILMath.cov(X.T);
            }

        }

    }

    public static class Dirichlet
    {
        public static ILRetArray<double> LogLikelihood(ILInArray<double> inAlpha, ILInArray<double> inProbabilitySample)
        {
            using (ILScope.Enter(inAlpha, inProbabilitySample))
            {
                ILArray<double> alpha = ILMath.check(inAlpha);
                ILArray<double> probabilitySample = ILMath.check(inProbabilitySample); 
                ILArray<double> L = 0;
                L = ILMath.sumall((alpha - (double)ILMath.ones(1, alpha.Length)) * ILMath.log(probabilitySample))
                     - Helper.sumLogGammaFunction(alpha) - Helper.logGammaFunction((ILMath.sumall(alpha)));
                return L;
            }
        }

        public static ILRetArray<double> Sample(ILInArray<double> inAlpha)
        {
            using (ILScope.Enter(inAlpha))
            {

                ILArray<double> alpha = ILMath.check(inAlpha);
                ILArray<double> probabilityVector = new double[alpha.Length];
                ILArray<double> betaSample = 0;
                ILArray<double> availableLength = 1;
                for (int i = 0; i < alpha.Length - 1; i++)
                {
                    betaSample = Beta.Sample(alpha[i], ILMath.sumall(alpha[ILMath.r(i + 1, ILMath.end)]));
                    probabilityVector[i] = availableLength * betaSample ;
                    availableLength = availableLength * (1 - betaSample);
                }
                probabilityVector[ILMath.end] = availableLength;

                return probabilityVector;
            }
        }

    }

    public static class Beta
    {
        public static ILRetArray<double> LogLikelihood(ILInArray<double> inSupport, ILInArray<double> inAlpha, ILInArray<double> inBeta)
        {
            using (ILScope.Enter(inSupport, inAlpha, inBeta))
            {
                ILArray<double> support = ILMath.check(inSupport);
                ILArray<double> alpha = ILMath.check(inAlpha);
                ILArray<double> beta = ILMath.check(inBeta);
                ILArray<double> L = 0;
                L = (alpha - ILMath.ones(alpha.Size)) * ILMath.sumall(ILMath.log(support))
                        + (beta - ILMath.ones(beta.Size)) * ILMath.sumall(ILMath.log(ILMath.ones(support.Size) - support))
                        - Helper.logBetaFunction(alpha, beta);
                return L;
            }
        }



        public static ILRetArray<double> Sample(ILInArray<double> inAlpha, ILInArray<double> inBeta)
        {
            using(ILScope.Enter(inAlpha, inBeta))
            {
                ILArray<double> alpha = ILMath.check(inAlpha);
                ILArray<double> beta = ILMath.check(inBeta);
                ILArray<double> sample = 0;
                ILArray<double> g1 = Gamma.Sample(alpha, 1.0);
                ILArray<double> g2 = Gamma.Sample(beta, 1.0);
                sample = g1 / (g1 + g2);
                return sample;
            }

        }
    }


    public static class Gamma
    {
        //Based on "A simple method for generating gamma variables" by Marsaglia G., Tsang W.
        // and the scaling property of Gamma distribution.
        public static ILRetArray<double> Sample(ILInArray<double> inAlpha, ILInArray<double> inBeta)
        {
            using (ILScope.Enter(inAlpha, inBeta))
            {
                ILArray<double> alpha = ILMath.check(inAlpha);
                ILArray<double> beta = ILMath.check(inBeta);
                if(alpha>1)
                {
                    ILArray<double> d = (alpha - 1) / 3;
                    ILArray<double> c = 1 / ILMath.sqrt(9 * d);
                    while (true)
                    {
                        ILArray<double> x = 0;
                        ILArray<double> v = 0;
                        while (v <= 0)
                        {
                            x = Gaussian.Sample(0, 1, 1);
                            v = 1 + c * x;
                        }
                        v = ILMath.pow(v, 3);
                        ILArray<double> u = ILMath.rand(1);
                        if (u < (1 - 0.0331 * ILMath.pow(x, 4)) 
                               || ILMath.log(u) < (0.5 * ILMath.pow(x,2) + d * (1 - v + ILMath.log(v))))
                        {
                            return d * v * beta ;
                        }
                    }
                    
                }
                else if (alpha > 0)
                {
                    return Gamma.Sample(alpha + 1, 1) * ILMath.pow(ILMath.rand(1), 1 / alpha) * beta;
                }
                else
                {
                    throw new Exception("Gamma.Sample: Invalid arguments");
                }
            }
        }
    }
    
    public static class StickBreakingConstruction
    {
        public static ILRetArray<double> BreakSticks(ILInArray<double> inAlpha, int numberOfSticks = 100)
        {
            using(ILScope.Enter(inAlpha))
            {
                ILArray<double> alpha = ILMath.check(inAlpha);
                ILArray<double> sticks = ILMath.zeros(numberOfSticks);
                sticks[0] = Beta.Sample(1,alpha);
                ILArray<double> cumProduct = (1 - sticks[0]);
                for(int i=1; i<numberOfSticks; i++){
                    ILArray<double> betaSample = Beta.Sample(1, alpha);
                    sticks[i] = betaSample * cumProduct;
                    cumProduct = cumProduct * (1 - betaSample);
                }
                return sticks;
            }
        }

    }


    public static class Wishart
    {
        public static ILRetArray<double> LogLikelihood(ILInArray<double> inP, ILInArray<double> inNu, ILInArray<double> inV)
        {
            using (ILScope.Enter(inP, inNu, inV))
            {
                ILArray<double> P = ILMath.check(inP);
                ILArray<double> nu = ILMath.check(inNu);
                ILArray<double> V = ILMath.check(inV);
                int L = ILMath.size(V, 1);
                ILArray<double> logLikelihood = 0;
                logLikelihood = -logNormalizer(nu, V) + 0.5 * (nu - L - 1) * Helper.logDeterminant(P)
                                    - 0.5 * ILMath.trace(ILMath.multiply(ILMath.pinv(V), P));
                return logLikelihood;
            }

        }

        private static ILRetArray<double> logNormalizer(ILInArray<double> inNu, ILInArray<double> inV)
        {
            using (ILScope.Enter(inNu, inV))
            {
                ILArray<double> nu = ILMath.check(inNu);
                ILArray<double> V = ILMath.check(inV);
                int L = ILMath.size(V, 1);
                ILArray<double> Z = 0.5 * nu * L * ILMath.log(2.0) + 0.5 * nu * Helper.logDeterminant(V)
                                    + Helper.logMultivariateGammaFunction(nu / 2, L);
                return Z;
            }
        }

        public static ILRetArray<double> Sample(ILInArray<int> inNu, ILInArray<double> inV)
        {
            using (ILScope.Enter(inNu, inV))
            {
                ILArray<int> nu = ILMath.check(inNu);
                ILArray<double> V = ILMath.check(inV);
                int L = ILMath.size(V, 1);
                ILArray<double> x = ILMath.zeros(L,L);
                x = Gaussian.Sample(ILMath.zeros(L).T, V, (int)nu);
                
                return ILMath.multiply(x.T, x);
            }
        }
    }

    public static class NormalWissart
    {
        public static ILRetArray<double> LogLikelihood(ILInArray<double> inMu, ILInArray<double> inPrecision, ILInArray<double> inMu0, ILInArray<double> inLambda, ILInArray<double> inW, ILInArray<double> inNu)
        {
            using (ILScope.Enter(inMu, inPrecision, inMu0, inLambda, inW, inNu))
            {
                ILArray<double> mu = ILMath.check(inMu);
                ILArray<double> precision = ILMath.check(inPrecision);
                ILArray<double> mu0 = ILMath.check(inMu0);
                ILArray<double> lambda = ILMath.check(inLambda);
                ILArray<double> W = ILMath.check(inW);
                ILArray<double> nu = ILMath.check(inNu);

                ILArray<double> logLikelihood = 0;
                logLikelihood = Gaussian.LogLikelihood(mu0, ILMath.pinv(precision) / lambda, mu.T)
                                    + Wishart.LogLikelihood(precision, nu, W);

                return logLikelihood;

            }
        }
    }

}
 