namespace Drowsy.Application.Games.DrowZzz.Influences
{
    /// <summary>
    /// <see cref="PlayerInfluence"/> 関連の定数集約(ドメイン上の真の不変量、階層 L2 相当)。
    /// </summary>
    /// <remarks>
    /// L2 の正規配置は Domain 層の `<Module>Constants` クラスだが、<see cref="PlayerInfluence"/> 自体が
    /// Application 層に存在するため、本定数も Application 層同パスに配置する。
    /// 性質上は L2(値オブジェクトに対する不変量)を表明し、Domain への移動は
    /// <see cref="PlayerInfluence"/> 移動と同時にのみ行う。
    /// </remarks>
    public static class InfluenceConstants
    {
        /// <summary>
        /// <see cref="PlayerInfluence.RemainingCount"/> に指定すると「実質永続」を意味するマジック値
        /// (<see cref="int.MaxValue"/>)。
        /// </summary>
        /// <remarks>
        /// <para>
        /// <see cref="PlayerInfluence"/> の <see cref="PlayerInfluence.RemainingCount"/> は 1 以上必須 / 0 到達で除去の
        /// 不変条件を持つため、「永続発動」を型レベルで表す手段がない。本定数は <see cref="int.MaxValue"/> を
        /// 「永続」セマンティクスのマジック値として標準化し、xmldoc 経由で意図を表明する妥協策。
        /// </para>
        /// <para>
        /// 実質枯渇しない根拠: 1 ゲームの上限 Tick 回数 = 全 21 ターン × N=2 = 42 回(<c>OwnPhaseStart</c> trigger は
        /// 1 フェーズに 1 回発火)。<see cref="int.MaxValue"/>(= 2,147,483,647)から 42 回 -1 しても 0 にならない
        /// ため、ゲーム終了まで生存する。Phase 3 で N>2 / Tick 多発トリガー拡張が起きても 9 桁の余裕がある。
        /// </para>
        /// <para>
        /// カード No.03「身体にいいもの」(影響 x/y が「自分のフェーズ開始時 SDP±N」永続発動)で本定数の必要性が顕在化。
        /// 型変更案(<c>int?</c> nullable 化 / abstract record 2 派生型分離)も検討したが、既存 <see cref="PlayerInfluence"/> 型 /
        /// Newtonsoft.Json default reflection 経由のセッション永続化 / SO 永続化 / 60+ Tests を維持できる本マジック値方式を採用。
        /// </para>
        /// </remarks>
        public const int Perpetual = int.MaxValue;
    }
}
