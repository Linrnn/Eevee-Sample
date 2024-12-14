using Eevee.Event;
using System;
using UnityEngine;
using UnityEngine.Profiling;

/// <summary>
/// Event示例代码
/// </summary>
internal sealed class EventSample : MonoBehaviour
{
    #region EventId
    private struct EventId
    {
        internal const int _11 = 11;
        internal const int _12 = 12;
        internal const int _13 = 13;
        internal const int _21 = 21;
        internal const int _22 = 22;
        internal const int _23 = 23;
    }
    #endregion

    #region Panel
    [Serializable]
    private sealed class HeroPanel
    {
        private readonly EventGroup _group = new();

        internal EventModule EmailEventModule;
        internal EventModule LoginEventModule;

        // 在监视面板查看结果
        [SerializeField] private int count1;
        [SerializeField] private int count2;
        [SerializeField] private int count3;

        internal void OnEnable()
        {
            _group.AddListener<StructContext>(EmailEventModule, EventId._11, AddCount1);
            _group.AddListener<ClassContext>(EmailEventModule, EventId._12, AddCount2);
            _group.AddListener(LoginEventModule, EventId._13, AddCount3); // 测试事件域隔离
        }
        internal void OnDisable()
        {
            _group.RemoveAllListener();

            count1 = 0;
            count2 = 0;
            count3 = 0;
        }

        #region AddCount
        private void AddCount1(StructContext context) => count1 += context.Count;
        private void AddCount2(ClassContext context) => count2 += context.Count;
        private void AddCount3() => ++count3;
        #endregion
    }

    [Serializable]
    private sealed class ItemPanel
    {
        private readonly EventGroup _group = new();

        internal EventModule EmailEventModule;
        internal EventModule LoginEventModule;

        // 在监视面板查看结果
        [SerializeField] private int count1;
        [SerializeField] private int count2;
        [SerializeField] private int count3;

        internal void OnEnable()
        {
            _group.AddListener<IEventContext>(EmailEventModule, EventId._21, AddCount1); // 测试事件域隔离
            _group.AddListener(LoginEventModule, EventId._22, AddCount2);
            _group.AddListener(LoginEventModule, EventId._23, AddCount3);
        }
        internal void OnDisable()
        {
            _group.RemoveAllListener();

            count1 = 0;
            count2 = 0;
            count3 = 0;
        }

        #region AddCount
        private void AddCount1(IEventContext _) => ++count1;
        private void AddCount2(IEventContext _) => ++count2;
        private void AddCount3() => ++count3;
        #endregion
    }
    #endregion

    #region EventContext
    private readonly struct StructContext : IEventContext
    {
        internal readonly int Count;

        internal StructContext(int count) => Count = count;
    }

    private sealed class ClassContext : IEventContext
    {
        internal readonly int Count;

        internal ClassContext(int count) => Count = count;
    }
    #endregion

    private readonly ClassContext _context = new(2);

    private readonly EventModule _emailEventModule = new();
    private readonly EventModule _loginEventModule = new();

    [SerializeField] private HeroPanel heroPanel = new(); // 在监视面板查看结果
    [SerializeField] private ItemPanel itemPanel = new(); // 在监视面板查看结果

    private void Awake()
    {
        heroPanel.EmailEventModule = _emailEventModule;
        heroPanel.LoginEventModule = _loginEventModule;

        itemPanel.EmailEventModule = _emailEventModule;
        itemPanel.LoginEventModule = _loginEventModule;
    }
    private void OnEnable()
    {
        heroPanel.OnEnable();
        itemPanel.OnEnable();
    }
    private void Update()
    {
        Profiler.BeginSample("EventSample.Update");

        _emailEventModule.Update();
        _loginEventModule.Update();

        _emailEventModule.Dispatch(EventId._11, new StructContext(3));
        _emailEventModule.Dispatch(EventId._12, _context);
        _emailEventModule.Dispatch(EventId._13); // _13是_loginEventModule注册的，所以无法接收到事件

        _loginEventModule.Enqueue(EventId._21, _context); // _21是_emailEventModule注册的，所以无法接收到事件
        _loginEventModule.Enqueue(EventId._22, _context);
        _loginEventModule.Enqueue(EventId._23, null);

        Profiler.EndSample();
    }
    private void OnDisable()
    {
        heroPanel.OnDisable();
        itemPanel.OnDisable();
    }
    private void OnDestroy()
    {
        _emailEventModule.Disable();
        _loginEventModule.Disable();
    }
}