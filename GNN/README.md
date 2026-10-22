# GNN Risk Haritası ve Puanlama — Elektrik Kaçağı

## Risk haritası (GraphSAGE)
Ofis zemini 0.5 m hücrelere bölünür; her hücre bir düğümdür. Etiketler (yalnızca eğitimde):

| Sınıf | Kural |
|---|---|
| Tehlikeli | Enerjili ekipman kutusuna (FalseBehaviourController: fiş, 3'lü priz) ≤ 1 m (NFPA 70E sınırlı yaklaşma ~1.07 m) |
| Dikkat | ≤ 2.5 m |
| Güvenli | Kalan alan |

Özellikler (8): `leak_distance`, `leak_path_distance` (masaları dolanarak), `equipment_distance`,
`cable_density`, `panel_distance`, `min_clearance`, `mean_clearance`, `openness`.

## Puanlama (100'den düşer)
| Hata | Ceza |
|---|---|
| Geç müdahale | İlk 15 s ücretsiz, sonra `30 × (1 − e^(−gecikme/30))`, en fazla 30 |
| 2 dk içinde enerji kesilmedi | Senaryo "Kaçak yangına dönüştü" ile biter: geç müdahale 30 + 30; o anki konum Dikkat −15, Tehlikeli −30 |
| Kaçağa yaklaşma (GNN Tehlikeli) | İlk 5 s (toplam) yalnızca uyarı; sonra ani düşüş olmadan kademeli, bütçe 30: `30 × (1 − e^(−E/5))`, E = 5 s'den sonraki süre. 6 s → −8, 8 s → −15, 10 s → −20, 15 s → −26. Dikkat alanı yalnızca uyarı |
| Enerjili fiş / prize dokunma | İlk temas −30, her yeni temas −10, temas sürdükçe −5/s (üst sınır yok); iki el aynı anda = tek temas |

Etiket: 75+ Güvenli, 50–74 Orta Riskli, 25–49 Riskli, 25 altı Yüksek Riskli.

## Unity'de yapılacaklar
1. Sınav sahnesinde (`VR-ASYS-ELEKTIRIK-SINAV`) boş bir obje `GNN` oluştur; `GnnRiskPredictor` ve `GnnRiskScorer` ekle. Sahneyi kaydet.
2. **Tools > GNN > Eğitim Verisini ve Grafı Dışa Aktar** çalıştır.
3. Eğitim:
   ```
   python3 -m venv GNN/.venv && GNN/.venv/bin/pip install -r GNN/requirements.txt && GNN/.venv/bin/python GNN/train_gnn.py
   ```
4. Haritayı kontrol et: `GNN` objesi seçiliyken sağ tık menüsünden *Kayıtlı grafla modeli çalıştır*.

Not: Sınav sahnesindeki Power Box (`MainSwitchboard`) objesinde collider yok; şalter kolları (`SwitchState`) zaten çalışıyor. Kutuya dokunarak da kapatılsın isteniyorsa trigger BoxCollider eklenmeli.
