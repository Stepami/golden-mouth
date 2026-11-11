import './feedFilter.css';

export function FeedFilter({ value, onChange }: {
  value: 'all' | 'mine';
  onChange: (value: 'all' | 'mine') => void;
}) {
  return <div className="feed-filter" role="group" aria-label="Публикации в ленте">
    <button type="button" aria-pressed={value === 'all'} onClick={() => onChange('all')}>Все</button>
    <button type="button" aria-pressed={value === 'mine'} onClick={() => onChange('mine')}>Мои посты</button>
  </div>;
}
