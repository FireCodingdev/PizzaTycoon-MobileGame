# Guia de Tuning da Animação do Player

Resolver: pés deslizando no chão, animação travando ("patinando").

## 1. Pés deslizando no chão (foot sliding)

Acontece quando a **velocidade visual da animação** não bate com a **velocidade do movimento**.

### Opção A — Deixar o código controlar tudo (atual)
No Inspector do Player → componente **PlayerAnimator**:
- `Scale Anim Speed With Move Speed` = **true** (default)
- Isso faz `animator.speed = velocidade_atual / _baseMoveSpeed`
- Ajuste `_baseMoveSpeed` para bater com a velocidade que a animação de andar/correr foi feita.
  - Se a animação foi feita pra um humanoide andando a ~5 m/s, deixe `_baseMoveSpeed = 5`.
  - Se mesmo assim desliza, mexa nesse valor até parar de deslizar.

### Opção B — Velocidade fixa de animação (mais previsível)
No Inspector do Player → **PlayerAnimator**:
- Desmarque `Scale Anim Speed With Move Speed`
- `Run Anim Speed` = `1.4` (animação 40% mais rápida ao correr)
- A animação sempre roda na velocidade nativa quando anda, e em `_runAnimSpeed` quando corre.
- **Vantagem:** não desliza nem em upgrades de velocidade. **Custo:** menos realista em velocidades muito altas.

### Opção C — Root Motion (mais realista)
- Selecione o Player no Hierarchy → componente **Animator**
- Marque **Apply Root Motion**
- Edite o `PlayerController.cs` para NÃO definir `rigidbody.velocity` diretamente — deixe a animação mover o personagem
- **Cuidado:** quebra o sistema atual de movimento. Só use se você quer reescrever o controller.

---

## 2. Animação "patinando" / travando

O personagem se move mas a animação congela ou volta pro Idle por um instante.

### Causa 1: Damp time alto demais
No **PlayerAnimator** Inspector:
- `Vert Damp` (novo campo) — default `0.1`
- Diminua para `0.05` ou `0` — transições ficam instantâneas
- Se a animação ainda parece "pular" entre estados, deixe em `0`

### Causa 2: Transições do AnimatorController com "Has Exit Time"
1. No **Project**, ache o **AnimatorController** do Player (.controller)
2. Clique 2x — abre a janela **Animator**
3. Clique em cada **seta** entre estados (Idle → Walk, Walk → Run, etc.)
4. No Inspector da transição:
   - **Has Exit Time** → DESMARQUE (a transição não espera a animação terminar)
   - **Transition Duration** → `0.1` (ou menos)
   - **Interruption Source** → `Current State`

### Causa 3: BlendTree com thresholds errados
Se o controller usa um BlendTree (Idle/Walk/Run):
1. No Animator, clique 2x no estado **Blend Tree**
2. Cheque que o parâmetro alimentado bate com o que o código envia:
   - O `PlayerAnimator` envia `Vert = 0` (parado) ou `Vert = 1` (andando)
   - O `State` é `0` (Idle), `1` (Walk) ou `2` (Run)
3. Se o BlendTree espera `Vert` indo de `0` até `1`, está OK.
   Se ele espera valores diferentes (ex: até `6`), ou ajuste o threshold ou troque o que o código envia em `PlayerAnimator.SetMovingFull`.

---

## 3. Debug rápido

Com o jogo rodando, abra **Window → Animation → Animator** (com o Player selecionado).
Você vê em tempo real:
- Qual estado está tocando (highlight azul)
- Os valores dos parâmetros (Vert, Hor, State, Speed)
- Quando uma transição dispara

Se a transição não dispara, é problema de **condition** (parâmetro errado ou valor não bate).
Se dispara mas volta logo, é problema de **Has Exit Time** ou parâmetro mudando rápido demais.
