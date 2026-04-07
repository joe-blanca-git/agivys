import { CommonModule } from '@angular/common';
import {
  Component,
  ElementRef,
  EventEmitter,
  Input,
  Output,
  ViewChild,
  OnInit,
} from '@angular/core';
import { FarmService } from '../../services/farm.service';
import { ToastService } from '../../../../../core/services/toast.service';
import { FarmPreviewMapComponent } from '../farm-preview-map/farm-preview-map.component';

@Component({
  selector: 'app-farm-new',
  standalone: true,
  imports: [CommonModule, FarmPreviewMapComponent],
  templateUrl: './farm-new.component.html',
  styleUrl: './farm-new.component.scss',
})
export class FarmNewComponent implements OnInit {
  @ViewChild(FarmPreviewMapComponent) previewMap!: FarmPreviewMapComponent;
  @ViewChild('fileDropRef') fileDropRef!: ElementRef<HTMLInputElement>;
  @Output() loadingEmit = new EventEmitter<boolean>();

  isDragging: boolean = false;
  isLoading: boolean = true;
  isLoadingUpload: boolean = false;

  selectedFiles: File[] = [];
  errorMessage: string = '';
  uploadedFiles: any;

  readonly allowedExtensions = ['.shp', '.shx', '.dbf', '.prj'];

  constructor(
    private farmService: FarmService,
    private toasService: ToastService,
  ) {}

  ngOnInit(): void {
    this.loadData();
  }

  loadData() {
    setTimeout(() => {
      this.isLoading = true;
      this.loadingEmit.emit(true);
    }, 0);

    setTimeout(() => {
      this.isLoading = false;
      this.loadingEmit.emit(false);
    }, 2000);
  }

  // --- FUNÇÃO PARA ENVIAR PARA A API PYTHON ---
  public submitBoundary(): void {
    // 1. Trava de segurança: se já estiver enviando, não faz nada
    if (this.isLoadingUpload) return; 

    if (this.errorMessage !== '' || this.selectedFiles.length < 4) {
      this.errorMessage = 'Verifique os arquivos obrigatórios antes de enviar.';
      return;
    }

    this.isLoadingUpload = true;
    this.loadingEmit.emit(true);

    const formData = new FormData();

    this.selectedFiles.forEach((file) => {
      formData.append('files', file);
    });

    // Chama o Service
    this.farmService.uploadBoundary(formData).subscribe({
      next: (response) => {
        console.log('Dados do Shapefile lidos com sucesso:', response);
        
        this.uploadedFiles = response;
        this.isLoadingUpload = false;
        this.loadingEmit.emit(false);
        
        // REMOVIDO: this.selectedFiles = []; (Assim o form não "reseta" visualmente)
        
        setTimeout(() => {
          if (this.previewMap && response.geojson) {
            // Agora passamos o geojson e a área total que veio da API
            this.previewMap.drawShapefile(response.geojson, response.area_total_ha);
          }
        }, 150);

        this.toasService.success('Upload concluído com sucesso!', 5000);
      },
      error: (erro) => {
        this.toasService.error('Erro ao se comunicar com a API: ' + erro.message, 5000);
        this.isLoadingUpload = false;
        this.loadingEmit.emit(false);
      },
    });
  }

  // --- EVENTOS DE DRAG AND DROP ---
  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = true;
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = false;
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = false;

    const files = event.dataTransfer?.files;
    if (files) {
      this.handleFiles(files);
    }
  }

  // --- EVENTO DE CLIQUE (SELEÇÃO NORMAL) ---
  onFileSelected(event: any): void {
    const files = event.target.files as FileList;
    if (files) {
      this.handleFiles(files);
    }

    if (this.fileDropRef) {
      this.fileDropRef.nativeElement.value = '';
    }
  }

  // --- LÓGICA DE VALIDAÇÃO E ARMAZENAMENTO ---
  private handleFiles(files: FileList): void {
    this.errorMessage = '';

    Array.from(files).forEach((file: File) => {
      const fileName = file.name.toLowerCase();

      const fileExtension = this.allowedExtensions.find((ext) =>
        fileName.endsWith(ext),
      );

      if (!fileExtension) {
        this.errorMessage = `Formato inválido detectado. Por favor, envie apenas arquivos ${this.allowedExtensions.join(', ')}.`;
        return;
      }

      const existingFileIndex = this.selectedFiles.findIndex((f) =>
        f.name.toLowerCase().endsWith(fileExtension),
      );

      if (existingFileIndex !== -1) {
        this.selectedFiles[existingFileIndex] = file;
      } else {
        this.selectedFiles.push(file);
      }
    });

    this.checkRequiredFiles();
  }

  public removeFile(index: number): void {
    this.selectedFiles.splice(index, 1);
    this.errorMessage = '';
    this.checkRequiredFiles();
  }

  private checkRequiredFiles(): void {
    if (this.selectedFiles.length === 0) {
      this.errorMessage = '';
      return;
    }

    const uploadedExts = this.selectedFiles.map((f) => {
      const parts = f.name.toLowerCase().split('.');
      return `.${parts[parts.length - 1]}`;
    });

    const hasShp = uploadedExts.includes('.shp');
    const hasShx = uploadedExts.includes('.shx');
    const hasDbf = uploadedExts.includes('.dbf');
    const hasPrj = uploadedExts.includes('.prj');

    if (!hasShp || !hasShx || !hasDbf || !hasPrj) {
      this.errorMessage =
        'Atenção: Para o talhão ser carregado corretamente, os arquivos .shp, .shx, .dbf e .prj precisam ser enviados juntos.';
    } else {
      this.errorMessage = '';
      
      // AUTO-SUBMIT: Validação passou, chama a API automaticamente!
      this.submitBoundary();
    }
  }

  public formatBytes(bytes: number, decimals = 2): string {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const dm = decimals < 0 ? 0 : decimals;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(dm)) + ' ' + sizes[i];
  }

  getStatusForm() {
    return !this.isLoading;
  }
}
