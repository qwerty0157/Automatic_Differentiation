using System;
using System.Collections.Generic;

namespace AutoDiff
{
    public class AD
    {
        /// <summary>
        /// この変数を入力として使っている演算の数
        /// </summary>
        private int UsedNum;

        /// <summary>
        /// 偏微分値の足しこみ回数
        /// </summary>
        private int CalculatedNum;

        /// <summary>
        /// 計算待機リスト
        /// </summary>
        private static readonly Queue<AD> CalcList = new Queue<AD>();

        /// <summary>
        /// 準備中フラグ
        /// </summary>
        private bool InPreparation;

        /// <summary>
        /// 変数値、演算の結果値
        /// </summary>
        public double Val { get; set; }

        /// <summary>
        /// 偏微分値
        /// </summary>
        public double Dif { get; private set; }

        /// <summary>
        /// 演算の入力
        /// </summary>
        private readonly AD[] Inputs;

        /// <summary>
        /// 入力変数による偏導関数値
        /// </summary>
        private readonly double[] Differentials;

        /// <summary>
        /// 変数を表すコンストラクタ
        /// </summary>
        /// <param name="v">変数値</param>
        public AD(double v)
        {
            this.Val = v;
            this.Inputs = null;
            this.Differentials = null;
        }

        /// <summary>
        /// 演算を表すコンストラクタ
        /// </summary>
        /// <param name="v">演算の結果値</param>
        /// <param name="inputNum">入力の数</param>
        public AD(double v, int inputNum)
        {
            this.Val = v;
            this.Inputs = new AD[inputNum];
            this.Differentials = new double[inputNum];
        }

        /// <summary>
        /// 微分計算
        /// </summary>
        public void GetDifferential()
        {
            // 待機リストが空になるまでループして全ノードで準備
            CalcList.Enqueue(this);

            while (CalcList.Count > 0)
            {
                CalcList.Dequeue().Prepare();
            }

            // 目的関数自身による微分値は1になる
            this.Dif = 1.0;

            // 待機リストが空になるまでループして全ノードで計算
            CalcList.Enqueue(this);

            while (CalcList.Count > 0)
            {
                CalcList.Dequeue().Calculate();
            }
        }

        /// <summary>
        /// 偏微分計算の準備
        /// </summary>
        private void Prepare()
        {
            // すでに計算準備を行っているなら何もしない
            if (this.InPreparation) return;
            this.InPreparation = true;

            this.Dif = 0;
            this.CalculatedNum = 0;

            if (this.Inputs == null) return;
            for (int i = 0; i < this.Inputs.Length; i++)
            {
                var src = this.Inputs[i];
                src.UsedNum++;

                if (!src.InPreparation)
                {
                    CalcList.Enqueue(src);
                }
            }
        }

        /// <summary>
        /// 偏微分値計算
        /// </summary>
        protected void Calculate()
        {
            // すでに偏微分値計算を行っているなら何もしない
            if (!this.InPreparation) return;
            this.InPreparation = false;

            if (this.Inputs == null) return;
            for (int i = 0; i < this.Inputs.Length; i++)
            {
                var src = this.Inputs[i];
                src.Dif += this.Dif * this.Differentials[i];
                src.CalculatedNum++;
                // 計算回数が演算ソースとして使われている回数に達した（＝微分値導出完了）なら待機リストに加える
                if (src.CalculatedNum >= src.UsedNum)
                {
                    src.UsedNum = 0;
                    CalcList.Enqueue(src);
                }
            }
        }

        /// <summary>
        /// 入力変数と対応する偏微分値を追加する
        /// </summary>
        public void AddInput(int index, AD input, double diff)
        {
            this.Inputs[index] = input;
            this.Differentials[index] = diff;
        }

        /// <summary>
        /// +演算子のオーバーロード
        /// </summary>
        public static AD operator +(AD x, AD y)
        {
            var z = new AD(x.Val + y.Val, 2);
            z.AddInput(0, x, 1);
            z.AddInput(1, y, 1);
            return z;
        }

        /// <summary>
        /// *演算子のオーバーロード
        /// </summary>
        public static AD operator *(AD x, AD y)
        {
            var z = new AD(x.Val * y.Val, 2);
            z.AddInput(0, x, y.Val);
            z.AddInput(1, y, x.Val);
            return z;
        }

        /// <summary>
        /// +単項演算子のオーバーロード
        /// </summary>
        public static AD operator +(AD x)
        {
            var z = new AD(x.Val, 1);
            z.AddInput(0, x, 1);
            return z;
        }

        /// <summary>
        /// -単項演算子のオーバーロード
        /// </summary>
        public static AD operator -(AD x)
        {
            var z = new AD(-x.Val, 1);
            z.AddInput(0, x, -1);
            return z;
        }

        /// <summary>
        /// -演算子のオーバーロード
        /// </summary>
        public static AD operator -(AD x, AD y)
        {
            var z = new AD(x.Val - y.Val, 2);
            z.AddInput(0, x, 1);
            z.AddInput(1, y, -1);
            return z;
        }

        /// <summary>
        /// /演算子のオーバーロード
        /// </summary>
        public static AD operator /(AD x, AD y)
        {
            var z = new AD(x.Val / y.Val, 2);
            z.AddInput(0, x, 1 / y.Val);
            z.AddInput(1, y, -x.Val / (y.Val * y.Val));
            return z;
        }

        /// <summary>
        /// 暗黙の型変換
        /// </summary>
        public static implicit operator AD(double v)
        {
            return new AD(v);
        }
    }

    static class ADMath
    {
        /// <summary>
        /// Exp関数
        /// </summary>
        public static AD Exp(AD x)
        {
            var z = new AD(Math.Exp(x.Val), 1);
            z.AddInput(0, x, z.Val);
            return z;
        }

        /// <summary>
        /// Sqrt関数
        /// </summary>
        public static AD Sqrt(AD x)
        {
            var z = new AD(Math.Sqrt(x.Val), 1);
            z.AddInput(0, x, 0.5 / z.Val);
            return z;
        }

        /// <summary>
        /// Log関数
        /// </summary>
        public static AD Log(AD x)
        {
            const double delta = 1e-13;
            var z = new AD(Math.Log(x.Val + delta), 1);
            z.AddInput(0, x, 1 / (x.Val + delta));
            return z;
        }

        /// <summary>
        /// Log関数
        /// </summary>
        public static AD Log(AD x, double a)
        {
            const double delta = 1e-13;
            var z = new AD(Math.Log(x.Val + delta, a), 1);
            z.AddInput(0, x, 1 / ((x.Val + delta) * Math.Log(a)));
            return z;
        }

        /// <summary>
        /// Sin関数
        /// </summary>
        public static AD Sin(AD x)
        {
            var z = new AD(Math.Sin(x.Val), 1);
            z.AddInput(0, x, Math.Cos(x.Val));
            return z;
        }

        /// <summary>
        /// Cos関数
        /// </summary>
        public static AD Cos(AD x)
        {
            var z = new AD(Math.Cos(x.Val), 1);
            z.AddInput(0, x, -Math.Sin(x.Val));
            return z;
        }

        /// <summary>
        /// Tan関数
        /// </summary>
        public static AD Tan(AD x)
        {
            var z = new AD(Math.Tan(x.Val), 1);
            double cos = Math.Cos(x.Val);
            z.AddInput(0, x, 1 / (cos * cos));
            return z;
        }

        /// <summary>
        /// Tanh関数
        /// </summary>
        public static AD Tanh(AD x)
        {
            var z = new AD(Math.Tanh(x.Val), 1);
            z.AddInput(0, x, 1 - z.Val * z.Val);
            return z;
        }

        /// <summary>
        /// 絶対値関数
        /// </summary>
        public static AD Abs(AD x)
        {
            var z = new AD(Math.Abs(x.Val), 1);
            z.AddInput(0, x, x.Val < 0 ? -1 : 1);
            return z;
        }

        /// <summary>
        /// Max関数
        /// </summary>
        public static AD Max(AD x, AD y)
        {
            return x.Val > y.Val ? +x : +y;
        }

        /// <summary>
        /// Min関数
        /// </summary>
        public static AD Min(AD x, AD y)
        {
            return x.Val < y.Val ? +x : +y;
        }

        /// <summary>
        /// 累乗関数 x^y
        /// </summary>
        public static AD Pow(AD x, AD y)
        {
            var z = new AD(Math.Pow(x.Val, y.Val), 2);
            z.AddInput(0, x, y.Val * Math.Pow(x.Val, y.Val - 1));
            z.AddInput(1, y, z.Val * Math.Log(x.Val));
            return z;
        }

        /// <summary>
        /// 平均関数
        /// </summary>
        public static AD Average(AD[] x)
        {
            var z = new AD(0, x.Length);
            for (int i = 0; i < x.Length; i++)
            {
                z.Val += x[i].Val;
                z.AddInput(i, x[i], 1.0 / x.Length);
            }
            z.Val /= x.Length;
            return z;
        }

        /// <summary>
        /// 合計関数
        /// </summary>
        public static AD Sum(AD[] x)
        {
            var z = new AD(0, x.Length);
            for (int i = 0; i < x.Length; i++)
            {
                z.Val += x[i].Val;
                z.AddInput(i, x[i], 1);
            }
            return z;
        }

        /// <summary>
        /// Sigmoid関数
        /// </summary>
        public static AD Sigmoid(AD x)
        {
            var z = new AD(1 / (1 + Math.Exp(-x.Val)), 1);
            z.AddInput(0, x, (1 - z.Val) * z.Val);
            return z;
        }

        /// <summary>
        /// Rectified Linear Unit
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static AD ReLU(AD x)
        {
            var z = new AD(Math.Max(0, x.Val), 1);
            z.AddInput(0, x, x.Val > 0 ? 1 : 0);
            return z;
        }

        /// <summary>
        /// 内積関数
        /// </summary>
        public static AD InnerProd(AD[] x, AD[] y)
        {
            var N = Math.Min(x.Length, y.Length);
            var z = new AD(0, 2 * N);
            for (int i = 0; i < N; i++)
            {
                z.Val += x[i].Val * y[i].Val;
                z.AddInput(i, x[i], y[i].Val);
                z.AddInput(i + N, y[i], x[i].Val);
            }
            return z;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            AD x = 1;
            var a = 2;
            var b = 3;

            // y = (a * exp(x)) * (exp(x) + b)
            var c = ADMath.Exp(x);
            var d = a * c;
            var e = c + b;
            var y = d * e;
            y.GetDifferential();

            // yとdy/dxを出力
            Console.WriteLine(y.Val + "\t" + x.Dif);

            // 中間変数に分けなくてもいける
            y = a * ADMath.Exp(x) * (ADMath.Exp(x) + b);
            y.GetDifferential();

            // yとdy/dxを出力
            Console.WriteLine(y.Val + "\t" + x.Dif);

            // y = 5x^2 をループで書く
            x = 3;
            y = 0;

            for (int i = 0; i < 5; i++)
            {
                y += x * x;
            }

            y.GetDifferential();

            // yとdy/dxを出力
            Console.WriteLine(y.Val + "\t" + x.Dif);

            // y = (x - 5)^2 の極小値を最急降下法で求める
            x = 20; // 適当な初期値

            y = (x - 5) * (x - 5);
            y.GetDifferential();

            while (x.Dif * x.Dif >= 1e-20)
            {
                x = x.Val - 0.1 * x.Dif; // xの更新

                y = (x - 5) * (x - 5);
                y.GetDifferential();
            }

            // yの極小値とその時のx
            Console.WriteLine(y.Val + "\t" + x.Val);

            Console.Read();
        }
    }
}