using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class RandomPersonTextureBehaviour : MonoBehaviour, Messages.IOnAfterDeserialise
{
    [SkipSerialisation]
    public bool PickRandomOnStart = true;
    public Texture2D[] Skins = Array.Empty<Texture2D>();
    public Texture2D Flesh;
    public Texture2D Bone;
    [SkipSerialisation]
    public float Scale = 1f;
    public UnityEvent OnAfterChange;
    public int ChosenIndex;
    private PersonBehaviour _person;
    private Sprite[] _originalLimbSprites;
    private bool _deserialized;

    public void OnAfterDeserialise(List<GameObject> gameObjects)
        => _deserialized = true;
    private void Start()
    {
        _person = GetComponent<PersonBehaviour>();

        var nextTextureButton = new ContextMenuButton("nextSkin", "Next texture", "Switches to the next texture", NextTexture)
        {
            LabelWhenMultipleAreSelected = "Next texture"
        };
        var previousTextureButton = new ContextMenuButton("previousSkin", "Previous texture", "Switches to the previous texture", PreviousTexture)
        {
            LabelWhenMultipleAreSelected = "Previous texture"
        };

        _originalLimbSprites = new Sprite[_person.Limbs.Length];
        for (int i = 0; i < _person.Limbs.Length; i++)
        {
            var limb = _person.Limbs[i];

            previousTextureButton.Condition = nextTextureButton.Condition = () => ButtonCondition(limb);
            limb.PhysicalBehaviour.ContextMenuOptions.Buttons.Insert(0, nextTextureButton);
            limb.PhysicalBehaviour.ContextMenuOptions.Buttons.Insert(1, previousTextureButton);

            _originalLimbSprites[i] = limb.SkinMaterialHandler.renderer.sprite;
        }

        if (!_deserialized && PickRandomOnStart)
            ChosenIndex = UnityEngine.Random.Range(0, Skins.Length);

        SetTexturesByIndex();
    }
    private bool ButtonCondition(LimbBehaviour limb)
    {
        foreach (var otherLimb in _person.Limbs)
            if (SelectionController.Main.SelectedObjects.Contains(otherLimb.PhysicalBehaviour))
                return otherLimb == limb;
        return true;
    }
    private void NextTexture()
    {
        if (++ChosenIndex >= Skins.Length)
            ChosenIndex = 0;
        SetTexturesByIndex();
    }
    private void PreviousTexture()
    {
        if (--ChosenIndex < 0)
            ChosenIndex = Skins.Length - 1;
        SetTexturesByIndex();
    }
    private void SetTexturesByIndex()
    {
        if (Skins.Length == 0)
            return;

        var skin = Skins[ChosenIndex];
        for (int i = 0; i < _person.Limbs.Length; i++)
        {
            var limb = _person.Limbs[i];
            var limbSprites = LimbSpriteCache.Instance.LoadFor(_originalLimbSprites[i], skin, Flesh, Bone, Scale);

            limb.SkinMaterialHandler.renderer.sprite = limbSprites.Skin;
            if (Flesh)
                limb.SkinMaterialHandler.renderer.material.SetTexture(ShaderProperties.Get("_FleshTex"), Flesh);
            if (Bone)
                limb.SkinMaterialHandler.renderer.material.SetTexture(ShaderProperties.Get("_BoneTex"), Bone);

            if (limb.TryGetComponent<ShatteredObjectSpriteInitialiser>(out var shatteredObjectSpriteInitialiser))
                shatteredObjectSpriteInitialiser.UpdateSprites(in limbSprites);
        }

        OnAfterChange?.Invoke();
    }
}
public class RandomPersonTextureChildBehaviour : MonoBehaviour
{
    public Sprite[] Sprites;

    private void Start()
    {
        var renderer = GetComponent<SpriteRenderer>();
        var randomTextures = GetComponentInParent<RandomPersonTextureBehaviour>();

        if (randomTextures.OnAfterChange == null)
            randomTextures.OnAfterChange = new UnityEvent();
        randomTextures.OnAfterChange.AddListener(() => renderer.sprite = Sprites[randomTextures.ChosenIndex]);
    }
}