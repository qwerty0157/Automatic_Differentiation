using System;
using System.Collections.Generic;

namespace AutoDiff
{
    public class AutoDiff
    {
        
        /// この変数を入力として使っている演算の数
        private int UsedNum;

        /// 偏微分値の足しこみ回数
        private int CalculatedNum;

        
        /// 計算待機リスト
        private static readonly Queue<AutoDiff> CalcList = new Queue<AutoDiff>();
        
        /// 準備中フラグ
        private bool InPreparation;
        
        /// 変数値、演算の結果値
        public double Val { get; set; }
        
        /// 偏微分値
        public double Dif { get; private set; }

        
        /// 演算の入力
        private readonly AutoDiff[] Inputs;

        /// 入力変数による偏導関数値
        private readonly double[] Differentials;

        /// 変数を表すコンストラクタ
        /// <param name="v">変数値</param>
        public AutoDiff(double v)
        {
            this.Val = v;
            this.Inputs = null;
            this.Differentials = null;
        }
         
        /// 演算を表すコンストラクタ
        /// <param name="v">演算の結果値</param>
        /// <param name="inputNum">入力の数</param>
        public AutoDiff(double v, int inputNum)
        {
            this.Val = v;
            this.Inputs = new AutoDiff[inputNum];
            this.Differentials = new double[inputNum];
        }
            
        /// 微分計算
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

        /// 偏微分計算の準備
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

        /// 偏微分値計算
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

        /// 入力変数と対応する偏微分値を追加する
        public void AddInput(int index, AutoDiff input, double diff)
        {
            this.Inputs[index] = input;
            this.Differentials[index] = diff;
        }

        /// +演算子のオーバーロード
        public static AutoDiff operator +(AutoDiff x, AutoDiff y)
        {
            var z = new AutoDiff(x.Val + y.Val, 2);
            z.AddInput(0, x, 1);
            z.AddInput(1, y, 1);
            return z;
        }

        /// *演算子のオーバーロード
        public static AutoDiff operator *(AutoDiff x, AutoDiff y)
        {
            var z = new AutoDiff(x.Val * y.Val, 2);
            z.AddInput(0, x, y.Val);
            z.AddInput(1, y, x.Val);
            return z;
        }

        /// +単項演算子のオーバーロード
        public static AutoDiff operator +(AutoDiff x)
        {
            var z = new AutoDiff(x.Val, 1);
            z.AddInput(0, x, 1);
            return z;
        }
        
        /// -単項演算子のオーバーロード
        public static AutoDiff operator -(AutoDiff x)
        {
            var z = new AutoDiff(-x.Val, 1);
            z.AddInput(0, x, -1);
            return z;
        }

        /// -演算子のオーバーロード
        public static AutoDiff operator -(AutoDiff x, AutoDiff y)
        {
            var z = new AutoDiff(x.Val - y.Val, 2);
            z.AddInput(0, x, 1);
            z.AddInput(1, y, -1);
            return z;
        }
        
        /// /演算子のオーバーロード
        public static AutoDiff operator /(AutoDiff x, AutoDiff y)
        {
            var z = new AutoDiff(x.Val / y.Val, 2);
            z.AddInput(0, x, 1 / y.Val);
            z.AddInput(1, y, -x.Val / (y.Val * y.Val));
            return z;
        }

        /// 暗黙の型変換
        public static implicit operator AutoDiff(double v)
        {
            return new AutoDiff(v);
        }
    }

    static class AutoDiffMath
    {
        /// Exp関数
        public static AutoDiff Exp(AutoDiff x)
        {
            var z = new AutoDiff(Math.Exp(x.Val), 1);
            z.AddInput(0, x, z.Val);
            return z;
        }

        /// Sqrt関数
        public static AutoDiff Sqrt(AutoDiff x)
        {
            var z = new AutoDiff(Math.Sqrt(x.Val), 1);
            z.AddInput(0, x, 0.5 / z.Val);
            return z;
        }
        
        /// Log関数
        public static AutoDiff Log(AutoDiff x)
        {
            const double delta = 1e-13;
            var z = new AutoDiff(Math.Log(x.Val + delta), 1);
            z.AddInput(0, x, 1 / (x.Val + delta));
            return z;
        }

        /// Log関数
        public static AutoDiff Log(AutoDiff x, double a)
        {
            const double delta = 1e-13;
            var z = new AutoDiff(Math.Log(x.Val + delta, a), 1);
            z.AddInput(0, x, 1 / ((x.Val + delta) * Math.Log(a)));
            return z;
        }
        
        /// Sin関数
        public static AutoDiff Sin(AutoDiff x)
        {
            var z = new AutoDiff(Math.Sin(x.Val), 1);
            z.AddInput(0, x, Math.Cos(x.Val));
            return z;
        }

        /// Cos関数
        public static AutoDiff Cos(AutoDiff x)
        {
            var z = new AutoDiff(Math.Cos(x.Val), 1);
            z.AddInput(0, x, -Math.Sin(x.Val));
            return z;
        }

        /// Tan関数
        public static AutoDiff Tan(AutoDiff x)
        {
            var z = new AutoDiff(Math.Tan(x.Val), 1);
            double cos = Math.Cos(x.Val);
            z.AddInput(0, x, 1 / (cos * cos));
            return z;
        }

        /// Tanh関数
        public static AutoDiff Tanh(AutoDiff x)
        {
            var z = new AutoDiff(Math.Tanh(x.Val), 1);
            z.AddInput(0, x, 1 - z.Val * z.Val);
            return z;
        }

        /// 絶対値関数
        public static AutoDiff Abs(AutoDiff x)
        {
            var z = new AutoDiff(Math.Abs(x.Val), 1);
            z.AddInput(0, x, x.Val < 0 ? -1 : 1);
            return z;
        }

        /// Max関数
        public static AutoDiff Max(AutoDiff x, AutoDiff y)
        {
            return x.Val > y.Val ? +x : +y;
        }

        /// Min関数
        public static AutoDiff Min(AutoDiff x, AutoDiff y)
        {
            return x.Val < y.Val ? +x : +y;
        }

        /// 累乗関数 x^y
        public static AutoDiff Pow(AutoDiff x, AutoDiff y)
        {
            var z = new AutoDiff(Math.Pow(x.Val, y.Val), 2);
            z.AddInput(0, x, y.Val * Math.Pow(x.Val, y.Val - 1));
            z.AddInput(1, y, z.Val * Math.Log(x.Val));
            return z;
        }
        
        /// 平均関数
        public static AutoDiff Average(AutoDiff[] x)
        {
            var z = new AutoDiff(0, x.Length);
            for (int i = 0; i < x.Length; i++)
            {
                z.Val += x[i].Val;
                z.AddInput(i, x[i], 1.0 / x.Length);
            }
            z.Val /= x.Length;
            return z;
        }

        /// 合計関数
        public static AutoDiff Sum(AutoDiff[] x)
        {
            var z = new AutoDiff(0, x.Length);
            for (int i = 0; i < x.Length; i++)
            {
                z.Val += x[i].Val;
                z.AddInput(i, x[i], 1);
            }
            return z;
        }

        /// Sigmoid関数
        public static AutoDiff Sigmoid(AutoDiff x)
        {
            var z = new AutoDiff(1 / (1 + Math.Exp(-x.Val)), 1);
            z.AddInput(0, x, (1 - z.Val) * z.Val);
            return z;
        }

        /// Rectified Linear Unit
        /// <param name="x"></param>
        /// <returns></returns>
        public static AutoDiff ReLU(AutoDiff x)
        {
            var z = new AutoDiff(Math.Max(0, x.Val), 1);
            z.AddInput(0, x, x.Val > 0 ? 1 : 0);
            return z;
        }
        
        /// 内積関数
        public static AutoDiff InnerProd(AutoDiff[] x, AutoDiff[] y)
        {
            var N = Math.Min(x.Length, y.Length);
            var z = new AutoDiff(0, 2 * N);
            for (int i = 0; i < N; i++)
            {
                z.Val += x[i].Val * y[i].Val;
                z.AddInput(i, x[i], y[i].Val);
                z.AddInput(i + N, y[i], x[i].Val);
            }
            return z;
        }
    }

}
