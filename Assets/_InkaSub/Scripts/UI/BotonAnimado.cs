using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Vida para los botones del menú: el elegido crece un poco, su texto se vuelve dorado y "respira".
// Pasar el mouse por encima lo elige (así mouse y teclado marcan el mismo botón).
public class BotonAnimado : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler
{
    public Text texto;
    public float escalaElegido = 1.07f;

    private bool elegido;
    private float escala = 1f;
    private static readonly Color Blanco = new Color(0.92f, 0.97f, 1f);

    void OnEnable() { elegido = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject; }

    public void OnSelect(BaseEventData e) { elegido = true; }
    public void OnDeselect(BaseEventData e) { elegido = false; }

    public void OnPointerEnter(PointerEventData e)
    {
        Selectable s = GetComponent<Selectable>();
        if (s != null && s.interactable && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(gameObject);
    }

    void Update()
    {
        float t = Time.unscaledTime;
        float meta = elegido ? escalaElegido + Mathf.Sin(t * 3f) * 0.012f : 1f;
        escala = Mathf.Lerp(escala, meta, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 14f));
        transform.localScale = Vector3.one * escala;
        if (texto != null) texto.color = Color.Lerp(texto.color, elegido ? UIInka.Dorado : Blanco, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 12f));
    }
}
