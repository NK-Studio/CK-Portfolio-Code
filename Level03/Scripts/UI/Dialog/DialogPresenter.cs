using System;
using Cysharp.Threading.Tasks;
using FMODPlus;
using Managers;
using Settings.UI;
using Sirenix.OdinInspector;
using UnityEngine;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.UI;

namespace Tutorial.Helper
{
	public class DialogPresenter : MonoBehaviour
	{
		public enum DialogActionType : byte
		{
			On,
			Off,
			OnOff,
			ChangeText,
		}
		[BoxGroup("동작"), SerializeField] 
		public DialogActionType Type = DialogActionType.OnOff;
	
		[BoxGroup("동작"), SerializeField, LabelText("UI 종류")] 
		private UIObjectType _uiObjectType = UIObjectType.RightSnowRabbitDialog;
	
		[BoxGroup("동작"), SerializeField, LabelText("대기 시간"), DisableIf("@Type == DialogActionType.ChangeText")] 
		private float _time = 4f;

		[BoxGroup("텍스트 설정"), SerializeField, LabelText("대상"), HideIf("@Type == DialogActionType.Off")]
		private TextMeshProUGUI _targetTextObject; // 현재 대사 출력 Text UI
		
		[BoxGroup("텍스트 설정"), SerializeField, LabelText("내용"), HideIf("@Type == DialogActionType.Off"), ReadOnly, TextArea] 
		private string _text; // 현재 분기의 대사 목록
		
		public string Text
		{
			get => _text;
			set => _text = value;
		}
		
		[BoxGroup("텍스트 설정"), SerializeField, LabelText("내용"), HideIf("@Type == DialogActionType.Off"), DrawWithUnity] 
		private LocalizedString _localizedText; // 현재 분기의 대사 목록
        [BoxGroup("텍스트 설정"), SerializeField, LabelText("사운드"), HideIf("@Type == DialogActionType.Off || _uiObjectType != UIObjectType.RightSnowRabbitDialog")] 
		private DialogTable.DialogSoundType _soundType = DialogTable.DialogSoundType.Su; // 현재 분기의 대사 목록
		
        
		private bool _completed = false;
		
		private void Start()
		{
			_localizedText.StringChanged += value =>
			{
				UpdateText();
			};
		}

		[BoxGroup("텍스트 설정"), Button, HideIf("@Type == DialogActionType.Off")]
		private void UpdateText()
		{
			var text = _localizedText.GetLocalizedString();
			_text = text;
		}

		public void Setup()
		{
			DialogSequence(_time).Forget();
		}

		public bool IsCompleted() => _completed;

		private async UniTaskVoid DialogSequence(float time)
		{
			_completed = false;
			if (Type != DialogActionType.Off && _targetTextObject)
			{
				_localizedText.RefreshString();
				_targetTextObject.spriteAsset = InputManager.Instance.CurrentKeychronSettings.SpriteAsset;
				_targetTextObject.text = _text;
				LayoutRebuilder.ForceRebuildLayoutImmediate(_targetTextObject.rectTransform);
				
				if(_uiObjectType == UIObjectType.RightSnowRabbitDialog)
					DialogManager.Instance.PlayDialogSound(_soundType);
				
			}

			if (Type == DialogActionType.OnOff)
			{
				// UI 켜기
				UIRenderer.Instance.ShowUI((int)_uiObjectType);
				// 딜레이 진행
				await UniTask.Delay(TimeSpan.FromSeconds(time));
				// UI 끄기
				UIRenderer.Instance.HideUI((int)_uiObjectType);
			}
			else if (Type == DialogActionType.On)
			{
				// UI 켜기
				UIRenderer.Instance.ShowUI((int)_uiObjectType);
				// 딜레이 진행
				await UniTask.Delay(TimeSpan.FromSeconds(time));
			}
			else if (Type == DialogActionType.Off)
			{
				// UI 끄기
				UIRenderer.Instance.HideUI((int)_uiObjectType);
				// 딜레이 진행
				await UniTask.Delay(TimeSpan.FromSeconds(time));
			}else if (Type == DialogActionType.ChangeText)
			{
				// Show/Hide 없이 그냥 딜레이 진행
				await UniTask.Delay(TimeSpan.FromSeconds(time));
			}
			_completed = true;
		
		}
	}
}