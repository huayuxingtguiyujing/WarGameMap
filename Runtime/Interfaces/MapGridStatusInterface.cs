namespace LZ.WarGameMap.Runtime
{
    /// <summary>
    /// 外部格子状态的类型约束。地图只保存运行时引用，不负责更新、存档或释放状态。
    /// 可按职责拆成多个实现，通常约三个，不设置硬上限。
    /// </summary>
    public interface MapGridStatusInterface
    {
    }
}
