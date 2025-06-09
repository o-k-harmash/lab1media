public class NeuronLayer
{
    public int NeuronCount;
    public int InputCount;
    public double[,] Weights;
    public double[] Biases;
    public double[] Outputs;
    public double[] Inputs;
    public double[] Deltas;

    public NeuronLayer(int inputCount, int neuronCount)
    {
        InputCount = inputCount;
        NeuronCount = neuronCount;
        Weights = new double[neuronCount, inputCount];
        Biases = new double[neuronCount];
        Outputs = new double[neuronCount];
        Inputs = new double[inputCount];
        Deltas = new double[neuronCount];
        InitWeights();
    }

    private void InitWeights()
    {
        Random rand = new Random();
        for (int i = 0; i < NeuronCount; i++)
        {
            for (int j = 0; j < InputCount; j++)
                Weights[i, j] = rand.NextDouble() * 2 - 1;
            Biases[i] = rand.NextDouble() * 2 - 1;
        }
    }

    public double[] FeedForward(double[] inputs, bool useSoftmax = false)
    {
        Inputs = inputs;
        double[] rawOutputs = new double[NeuronCount];

        for (int i = 0; i < NeuronCount; i++)
        {
            double sum = Biases[i];
            for (int j = 0; j < InputCount; j++)
                sum += Weights[i, j] * inputs[j];

            rawOutputs[i] = sum;
        }

        if (useSoftmax)
            Outputs = Softmax(rawOutputs);
        else
        {
            for (int i = 0; i < NeuronCount; i++)
                Outputs[i] = Sigmoid(rawOutputs[i]);
        }

        return Outputs;
    }

    public static double[] Softmax(double[] x)
    {
        double max = x.Max();
        double sum = 0.0;
        double[] result = new double[x.Length];
        for (int i = 0; i < x.Length; i++)
            sum += Math.Exp(x[i] - max); // для численной стабильности

        for (int i = 0; i < x.Length; i++)
            result[i] = Math.Exp(x[i] - max) / sum;

        return result;
    }

    public static double Sigmoid(double x) => 1.0 / (1.0 + Math.Exp(-x));
    public static double SigmoidDerivative(double x) => x * (1 - x);
}
