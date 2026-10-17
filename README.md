# Find the Ghost

Projeto para a GameJam+ 2026/2027.

## Loop de salas

O `GameController` carrega primeiro a sala inicial. Ao atravessar o gatilho de saída, o jogador percorre as salas de desafio na ordem configurada. Cada sala começa com três tentativas:

- Interagir com um objeto comum o quebra e consome uma tentativa.
- Esgotar as tentativas retorna o jogador à sala inicial e reinicia a sequência.
- Em uma sala assombrada, há exatamente um objeto marcado como alvo. Interagir com ele solta o fantasma; enquanto ele estiver solto, os outros objetos não podem ser selecionados ou usados.
- Capturar o fantasma libera a sala. Sair de uma sala assombrada sem capturá-lo conta como falha e retorna à sala inicial.
- Sair de uma sala não assombrada avança normalmente. Concluir a última sala dispara o evento `GameCompleted`.

## Configuração no Unity Editor

### 1. Preparar os objetos interativos

Em cada objeto que pode ser clicado:

1. Adicione `DebuggingObject`, `OutlineHighlight` e um `Collider`.
2. Configure o material de contorno no `OutlineHighlight`.
3. Deixe `Is Haunted` desmarcado nos objetos comuns.
4. No único objeto assombrado de cada sala assombrada, marque `Is Haunted` e atribua o prefab do fantasma a `Ghost Prefab`.
5. Coloque os objetos em uma layer incluída no `Interaction Layer Mask` do `PlayerController`.

O prefab do fantasma precisa ter um `Renderer`, `NavMeshAgent` e `GhostBehavior`. O script cria um collider de interação se o prefab não tiver collider. Garanta que o NavMesh da sala esteja assado e que o agente possa navegar nele.

### 2. Criar um prefab de sala

1. Crie o objeto raiz da sala e adicione `RoomInstance`.
2. Crie um objeto filho vazio no local e orientação em que o jogador deve aparecer; arraste-o para `Player Spawn Point`.
3. Crie a saída da sala com um `Collider` marcado como `Is Trigger` e adicione `RoomExit`. O gatilho precisa detectar o collider do jogador, que deve estar marcado com a tag `Player`.
4. Adicione ao prefab os objetos interativos preparados acima.
5. Transforme a sala em prefab.

Faça isso também para a sala inicial. A sala inicial normalmente deve ser configurada como não assombrada.
Os prefabs são instanciados na origem do mundo; mantenha o conteúdo da sala posicionado corretamente em relação à raiz do prefab. Remova ou desative a geometria de teste que já esteja fixa na cena para não sobrepô-la às salas instanciadas.

### 3. Criar os dados de sala

No Project, use **Create > Find The Ghost > Room** para criar um `RoomData` por sala. Em cada asset, configure:

- `Room Prefab`: o prefab criado no passo anterior.
- `Is Haunted`: marque somente nas salas que terão um fantasma.

Toda sala assombrada precisa ter exatamente um `DebuggingObject` marcado como `Is Haunted` no prefab. Salas não assombradas não soltam fantasmas, mesmo que tenham objetos com essa marca no prefab.

### 4. Configurar a cena

1. Mantenha um único objeto com `GameController` na cena.
2. Arraste o asset da sala inicial para `Starting Room`.
3. Adicione os assets de desafio ao array `Challenge Rooms`, na ordem desejada.
4. Deixe `Attempts Per Room` em `3` ou ajuste conforme necessário.
5. Arraste o objeto do jogador para `Player`. O jogador precisa ter `PlayerController`, `CharacterController` e a tag `Player`.
6. Configure o `Interaction Layer Mask` do jogador para incluir as layers dos objetos interativos e do fantasma.

As notificações `AttemptsChanged`, `GameLost` e `GameCompleted` estão disponíveis como eventos C# para conectar uma interface de tentativas, derrota ou conclusão. O fluxo principal funciona sem UI.
