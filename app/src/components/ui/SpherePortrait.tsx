interface SpherePortraitProps {
  src: string
  name?: string
  size?: 'sm' | 'md' | 'lg'
  className?: string
}

const sizeMap = {
  sm: 'h-14 w-14',
  md: 'h-20 w-20',
  lg: 'h-24 w-24',
}

const labelMap = {
  sm: 'text-xs',
  md: 'text-sm',
  lg: 'text-base',
}

export function SpherePortrait({ src, name, size = 'md', className }: SpherePortraitProps) {
  return (
    <div className={className}>
      <div className="flex flex-col items-center gap-2">
        <div
          className={`relative ${sizeMap[size]} shrink-0 rounded-full p-[2px] shadow-[0_8px_24px_rgba(0,0,0,0.45),inset_0_2px_4px_rgba(255,255,255,0.25)] ring-2 ring-white/20`}
          style={{
            background:
              'linear-gradient(145deg, rgba(255,255,255,0.35) 0%, rgba(255,195,0,0.45) 45%, rgba(0,0,0,0.15) 100%)',
          }}
        >
          <div className="relative h-full w-full overflow-hidden rounded-full bg-black/10">
            <img
              src={src}
              alt={name ?? ''}
              className="h-full w-full object-contain object-center"
            />
          </div>
        </div>
        {name ? (
          <span className={`font-semibold text-white drop-shadow ${labelMap[size]}`}>{name}</span>
        ) : null}
      </div>
    </div>
  )
}
