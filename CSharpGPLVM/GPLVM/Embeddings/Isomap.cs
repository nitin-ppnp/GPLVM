using System.Linq;
using ILNumerics;

namespace GPLVM.Embeddings
{
    public static partial class Embed
    {
        private struct Xstruct
        {
            public ILArray<int> index;
            public ILCell coords;
        };

        /// <summary>
        /// Embed data set with Isomap.
        /// </summary>
        /// <remarks>
        /// Isomap is used for computing a quasi-isometric, low-dimensional embedding of a set 
        /// of high-dimensional data points. The algorithm provides a simple method for 
        /// estimating the intrinsic geometry of a data manifold based on a rough 
        /// estimate of each data point’s neighbors on the manifold. Isomap is highly efficient and 
        /// generally applicable to a broad range of data sources and dimensionalities.
        /// 
        /// Translation of Tenenbaum, de Silva, and Langford' Matlab code.
        /// </remarks>
        /// <param name="inY">N by D data matrix.</param>
        /// <param name="numNeighbors">Number of neighbors.</param>
        /// <param name="q">Max embedding dimensionality.</param>
        /// <returns>
        /// The method returns the N by q latent matrix of type ILArray<double>.
        /// </returns>
        public static ILRetArray<double> Isomap(ILInArray<double> inY, int q)
        {
            using (ILScope.Enter(inY))
            {
                ILArray<double> Y = ILMath.check(inY);
                ILArray<double> X = ILMath.zeros(Y.Size[0], q);

                ILArray<double> D = L2_distance(Y.T, Y.T, true);

                int neighbours = 12;

                Xstruct xstruct = isomap(D, neighbours, q);//, options);

                if (xstruct.index.Length != Y.Size[0])
                {
                    // We don't really deal with this problem correctly here ...
                    System.Console.WriteLine("Isomap graph is not fully connected");
                }

                X[xstruct.index, ILMath.full] = xstruct.coords.GetArray<double>(q - 1).T;

                return X;
            }
        }

        private static Xstruct isomap(ILInArray<double> inD, int n_size, int q)
        {
            ILArray<double> D = ILMath.check(inD);
            Xstruct X;

            int N = D.Size[0]; 
            int K = n_size; 
                
            double INF =  (double)(1000 * ILMath.max(ILMath.max(D)) * N);  // effectively infinite distance

            System.Console.WriteLine("Isomap running on {0} points", N);

            ILArray<double> dims = ILMath.zeros(q, 1);
            for (int i = 0; i < q; i++)
                dims[i] = i;
           
            int comp = 0; 
            bool verbose = true; 

            X.coords = ILMath.cell(dims.Length, 1);
            ILArray<double> R = ILMath.zeros(1, dims.Length); 

            // Step 1: Construct neighborhood graph
            System.Console.WriteLine("\t-->Constructing neighborhood graph..."); 

            ILArray<int> perm = ILMath.empty<int>();
            ILArray<double> tmp = ILMath.sort(D, perm); 
            for (int i = 0; i < N; i++)
                D[i, perm[ILMath.r(1+K, ILMath.end),i]] = INF; 

            D = ILMath.min(D, D.T);    // Make sure distance matrix is symmetric

            //if (overlay)
            //    int8 E = int8(1-(D==INF));  //  Edge information for subsequent graph overlay

            // Finite entries in D now correspond to distances between neighboring points. 
            // Infinite entries (really, equal to INF) in D now correspond to 
            //   non-neighoring points. 

            // Step 2: Compute shortest paths
            System.Console.WriteLine("\t-->Computing shortest paths..."); 

            // We use Floyd's algorithm, which produces the best performance in Matlab. 
            // Dijkstra's algorithm is significantly more efficient for sparse graphs, 
            // but requires for-loops that are very slow to run in Matlab.  A significantly 
            // faster implementation of Isomap that calls a MEX file for Dijkstra's 
            // algorithm can be found in isomap2.m (and the accompanying files
            // dijkstra.c and dijkstra.dll). 

            for (int k = 0; k < N; k++)
            {
                    D = ILMath.min(D, ILMath.repmat(D[ILMath.full,k], 1, N) + ILMath.repmat(D[k,ILMath.full], N, 1)); 
                    if (verbose && (k % 20 == 0) && k != 0) 
                        System.Console.WriteLine("Iteration: " + k.ToString());
            }

            // Remove outliers from graph
            System.Console.WriteLine("\n\t-->Checking for outliers..."); 
            ILArray<double> n_connect = ILMath.sum(ILMath.todouble(!(D==INF))); // number of points each point connects to

            ILArray<int> firsts = ILMath.empty<int>();
            tmp = ILMath.min(ILMath.todouble(D==INF), firsts);      // first point each point connects to

            ILArray<int> comps = firsts.Distinct().ToArray();    // represent each connected component once
            ILArray<double> size_comps = n_connect[comps];          // size of each connected component
                
            ILArray<int> comp_order = ILMath.empty<int>();
            tmp = ILMath.sort(size_comps, comp_order);              // sort connected components by size
            comps = comps[comp_order[ILMath.r(ILMath.end, -1, 0)]];    

            size_comps = size_comps[comp_order[ILMath.r(ILMath.end, -1, 0)]]; 
            int n_comps = comps.Length;               // number of connected components
            if (comp > n_comps)                
                    comp = 0;                              // default: use largest component

            System.Console.WriteLine("\n  Number of connected components in graph: {0}", n_comps); 
            System.Console.WriteLine("  Embedding component {0} with {1} points.", comp, size_comps[comp]); 

            X.index = ILMath.find(firsts==comps[comp]); 

            D = D[X.index, X.index]; 
            N = X.index.Length; 

            // Step 3: Construct low-dimensional embeddings (Classical MDS)
            System.Console.WriteLine("\t-->Constructing low-dimensional embeddings (Classical MDS)..."); 

            //opt.disp = 0;

            ILArray<double> vec = ILMath.empty<double>(); 
            ILArray<double> val = ILMath.eigSymm(-.5 * (ILMath.pow(D, 2) 
                - ILMath.multiply(ILMath.sum(ILMath.pow(D, 2)).T, ILMath.ones(1, N) / N) 
                - ILMath.multiply(ILMath.ones(N,1), ILMath.sum(ILMath.pow(D, 2)) / N)
                + ILMath.sum(ILMath.sum(ILMath.pow(D, 2))) / (N + N)), vec);

            ILArray<double> h = ILMath.diag(val); 

            ILArray<int> sorth = ILMath.empty<int>();
            ILArray<double> foo = ILMath.sort(h, sorth);
            sorth = sorth[ILMath.r(ILMath.end, -1, sorth.Length - q)]; 

            val = ILMath.diag(val[sorth,sorth]); 
            vec = vec[ILMath.full,sorth]; 

            D = ILMath.reshape(D, N * N, 1); 
            ILArray<double> r2;
            for (int di = 0; di < dims.Length; di++)
            {
                    if (dims[di] <= N)
                    {
                        X.coords[di] = ILMath.multiplyElem(vec[ILMath.full, ILMath.r(0, dims[di])], 
                            ILMath.multiply(ILMath.ones(N, 1), ILMath.sqrt(val[ILMath.r(0, dims[di])]).T)).T;

                        r2 = 1 - ILMath.pow(Util.CorrCoef(ILMath.reshape(L2_distance(X.coords.GetArray<double>(di), 
                                                                X.coords.GetArray<double>(di), false), N * N, 1), D), 2); 
                        R[di] = r2[1,0];
                        if (verbose)
                            System.Console.WriteLine("  Isomap on {0} points with dimensionality {1} --> residual variance = {2}", N, dims[di], R[di]); 
                         
                    }
            }
            return X;
        }

        private static ILRetArray<double> L2_distance(ILInArray<double> inA, ILInArray<double> inB, bool df)
        {
            using (ILScope.Enter(inA, inB))
            {
                ILArray<double> a = ILMath.check(inA);
                ILArray<double> b = ILMath.check(inB);

                ILArray<double> aa = ILMath.sum(ILMath.multiplyElem(a, a)); 
                ILArray<double> bb = ILMath.sum(ILMath.multiplyElem(b, b)); 
                ILArray<double> ab = ILMath.multiply(a.T,b);
 
                ILArray<double> d = ILMath.sqrt(ILMath.repmat(aa.T, 1, bb.Size[1]) + 
                    ILMath.repmat(bb, aa.Size[1], 1) - 2 * ab);

                // force 0 on the diagonal? 
                if (df)
                  d = ILMath.multiplyElem(d, (1 - ILMath.eye(d.Size[0], d.Size[1])));

                return d;
            }
        }   

    }
}
