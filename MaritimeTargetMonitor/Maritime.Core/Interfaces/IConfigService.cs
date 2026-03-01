namespace Maritime.Core.Interfaces
{
    public interface IConfigService
    {
        T GetConfig<T>(string key);
        void SetConfig<T>(string key, T value);
        void LoadConfig();
        void SaveConfig();
    }
}
