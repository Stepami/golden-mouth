import './feedFilter.css';

export function ViewFilter<T extends string>({value,onChange,options,label}:{value:T;onChange:(value:T)=>void;options:readonly {value:T;label:string}[];label:string}) {
  return <div className="feed-filter" role="group" aria-label={label}>{options.map(option=><button key={option.value} type="button" aria-pressed={value===option.value} onClick={()=>onChange(option.value)}>{option.label}</button>)}</div>;
}

export function FeedFilter({value,onChange}:{value:'all'|'mine';onChange:(value:'all'|'mine')=>void}) {
  return <ViewFilter value={value} onChange={onChange} options={[{value:'all',label:'Все'},{value:'mine',label:'Мои посты'}]} label="Публикации в ленте"/>;
}
