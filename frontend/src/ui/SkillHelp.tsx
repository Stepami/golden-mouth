import { Button, Sheet } from '.'

export function SkillHelp({ onClose }: { onClose: () => void }) {
  return <Sheet title="Как поднять навыки" className="skill-help-sheet" onClose={onClose}>
    <ul className="skill-help-list"><li><b>🔥 Смелость</b><span>Делись опытом и отвечай по делу</span></li><li><b>😂 Юмор</b><span>Находи смешное в событиях мира</span></li><li><b>🎯 В тему</b><span>Пиши о героях и событиях своего мира</span></li></ul>
    <p className="small muted" style={{ margin: '12px 0' }}>Опыт за пост дают первые 10 единиц дневного расхода энергии; ответы в треде тоже расходуют этот бюджет. Первые 3 единицы дают ×2 опыта, выполненное желание — ×1,3. Главный навык получает весь опыт, остальные — по 15%. Даже неудачный пост помогает расти.</p>
    <Button onClick={onClose}>Понятно</Button>
  </Sheet>
}
